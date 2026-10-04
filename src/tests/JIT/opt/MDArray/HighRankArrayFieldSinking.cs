// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Preserve evaluation order when sinking loads of instance fields into high-rank array stores.
public sealed partial class HighRankArrayFieldSinking
{
    private const MethodImplOptions Options = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    private int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] _array = null!;
    private int _i0;
    private int _i1;
    private int _i2;
    private int _i3;
    private int _i4;
    private int _i5;
    private int _i6;
    private int _i7;
    private int _i8;
    private int _i9;
    private int _i10;
    private int _i11;
    private int _i12;
    private int _i13;
    private int _i14;
    private int _i15;
    private int _i16;
    private int _i17;
    private int _i18;
    private int _i19;
    private int _i20;
    private int _i21;
    private int _i22;
    private int _i23;
    private int _i24;
    private int _i25;
    private int _i26;
    private int _i27;
    private int _i28;
    private int _i29;
    private int _i30;
    private int _i31;
    private volatile int _volatileIndex;
    private int _value, _mutatedFirst, _observed;
    private string _trace = "";
    private HighRankArrayFieldSinking _other = null!;
    private int[] _originalIndices = null!;
    private Array _expected = null!;
    [MethodImpl(Options)]
    private void OwnFieldsSet()
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, _i31] = _value;
    }

    [MethodImpl(Options)]
    private void LateMutationSet()
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, MutateEarlierIndex()] = _value;
    }

    [MethodImpl(Options)]
    private void VolatileIndexSet()
    {
        _array[_volatileIndex, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, _i31] = _value;
    }

    [MethodImpl(Options)]
    private void OtherReceiverSet(HighRankArrayFieldSinking? other)
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, other!._i31] = _value;
    }

    [MethodImpl(Options)]
    private void RedefinedBaseSet()
    {
        HighRankArrayFieldSinking source = this;
        source._array[source._i0, source._i1, source._i2, source._i3, source._i4, source._i5, source._i6, source._i7, source._i8, source._i9, source._i10, source._i11, source._i12, source._i13, source._i14, source._i15, source._i16, source._i17, source._i18, source._i19, source._i20, source._i21, source._i22, source._i23, source._i24, source._i25, source._i26, source._i27, source._i28, source._i29, source._i30, (source = _other)._i31] = source._value;
    }

    [MethodImpl(Options)]
    private int CatchObservableFailure()
    {
        int before = _i0;
        try
        {
            _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, MutateBeforeFailure()] = _value;
        }
        catch (IndexOutOfRangeException)
        {
            _observed = _i0;
            _trace += "C";
        }

        return before;
    }

    [MethodImpl(Options)]
    private int MutateEarlierIndex()
    {
        _trace += "M";
        _i0 = _mutatedFirst;
        _value = 83;
        return _i31;
    }

    [MethodImpl(Options)]
    private int MutateBeforeFailure()
    {
        _trace += "M";
        _i0 = _mutatedFirst;
        _value = 83;
        return _array.GetUpperBound(31) + 1;
    }

    private static HighRankArrayFieldSinking Create(bool nonzero, int seed = 7)
    {
        int[] lengths = Enumerable.Repeat(1, 32).ToArray();
        lengths[0] = 3;
        lengths[16] = 3;
        lengths[31] = 4;
        int[] lower = Enumerable.Range(0, 32).Select(d => nonzero ? d - 16 : 0).ToArray();
        Array a = Array.CreateInstance(typeof(int), lengths, lower);
        int[] i = (int[])lower.Clone();
        for (int offset = 0; offset < a.Length; offset++)
        {
            a.SetValue(seed + offset * 17, i);
            Next(a, i);
        }

        i = (int[])lower.Clone();
        i[0]++;
        i[16]++;
        i[31]++;
        var result = new HighRankArrayFieldSinking
        {
            _array = (int[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,])a,
            _originalIndices = i,
            _expected = (Array)a.Clone(),
            _value = 37,
            _mutatedFirst = i[0] + 1,
            _volatileIndex = i[0] + 1
        };
        result._i0 = i[0];
        result._i1 = i[1];
        result._i2 = i[2];
        result._i3 = i[3];
        result._i4 = i[4];
        result._i5 = i[5];
        result._i6 = i[6];
        result._i7 = i[7];
        result._i8 = i[8];
        result._i9 = i[9];
        result._i10 = i[10];
        result._i11 = i[11];
        result._i12 = i[12];
        result._i13 = i[13];
        result._i14 = i[14];
        result._i15 = i[15];
        result._i16 = i[16];
        result._i17 = i[17];
        result._i18 = i[18];
        result._i19 = i[19];
        result._i20 = i[20];
        result._i21 = i[21];
        result._i22 = i[22];
        result._i23 = i[23];
        result._i24 = i[24];
        result._i25 = i[25];
        result._i26 = i[26];
        result._i27 = i[27];
        result._i28 = i[28];
        result._i29 = i[29];
        result._i30 = i[30];
        result._i31 = i[31];
        return result;
    }

    private static void VerifyAll()
    {
        foreach (bool nonzero in new[]
        {
            false,
            true
        }

        )
        {
            if (nonzero && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                continue;
            }

            HighRankArrayFieldSinking s = Create(nonzero);
            s.OwnFieldsSet();
            s.ExpectStore(s._originalIndices, 37);
            s.CheckArray("own fields");
            s = Create(nonzero);
            s.LateMutationSet();
            s.ExpectStore(s._originalIndices, 83);
            Equal(s._mutatedFirst, s._i0, "late mutation applied");
            Equal("M", s._trace, "late mutation trace");
            s.CheckArray("late mutation retains earlier index");
            s = Create(nonzero);
            int[] volatileIndices = (int[])s._originalIndices.Clone();
            volatileIndices[0] = s._volatileIndex;
            s.VolatileIndexSet();
            s.ExpectStore(volatileIndices, 37);
            s.CheckArray("volatile index");
            s = Create(nonzero);
            HighRankArrayFieldSinking other = Create(nonzero, 101);
            other._i31++;
            int[] mixed = (int[])s._originalIndices.Clone();
            mixed[31] = other._i31;
            s.OtherReceiverSet(other);
            s.ExpectStore(mixed, 37);
            s.CheckArray("different receiver");
            other.CheckArray("different receiver unchanged");
            // Evaluating the null receiver precedes the eventual MD-array bounds check.
            s = Create(nonzero);
            s._i0 = s._array.GetUpperBound(0) + 1;
            Throws<NullReferenceException>(() => s.OtherReceiverSet(null));
            s.CheckArray("null other beats invalid earlier index");
            s = Create(nonzero);
            other = Create(nonzero, 101);
            other._i31++;
            other._value = 211;
            s._other = other;
            mixed = (int[])s._originalIndices.Clone();
            mixed[31] = other._i31;
            s.RedefinedBaseSet();
            s.ExpectStore(mixed, 211);
            s.CheckArray("base redefinition retains original array and earlier indices");
            other.CheckArray("replacement base array unchanged");
            s = Create(nonzero);
            int originalFirst = s._originalIndices[0];
            Equal(originalFirst, s.CatchObservableFailure(), "catch retained earlier local");
            Equal(s._mutatedFirst, s._observed, "catch sees completed field mutation");
            Equal("MC", s._trace, "catch trace");
            s.CheckArray("failed Set does not write");
        }

        Console.WriteLine("PASS: HighRankArrayFieldSinking instance-field Set controls (zero/nonzero bounds): own fields, late mutation, volatile index, other/null receiver, base redefinition, observable catch.");
    }

    private void ExpectStore(int[] i, int value) => _expected.SetValue(value, i);
    private void CheckArray(string label)
    {
        Array actual = _array;
        int[] i = Enumerable.Range(0, 32).Select(actual.GetLowerBound).ToArray();
        for (int offset = 0; offset < actual.Length; offset++)
        {
            Equal((int)_expected.GetValue(i)!, (int)actual.GetValue(i)!, $"{label}, offset {offset}");
            Next(actual, i);
        }
    }

    private static void Next(Array a, int[] i)
    {
        for (int d = i.Length - 1; d >= 0; d--)
        {
            if (++i[d] <= a.GetUpperBound(d))
                return;
            i[d] = a.GetLowerBound(d);
        }
    }

    private static void Equal<T>(T expected, T actual, string label)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new Exception($"HighRankArrayFieldSinking {label}: expected {expected}, got {actual}.");
    }

    private static void Throws<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new Exception($"HighRankArrayFieldSinking expected {typeof(T).Name}.");
    }
}

public sealed partial class HighRankArrayFieldSinking
{
    private int _divisor;
    // Earlier _i0 must be captured before this late assignment overwrites it.
    [MethodImpl(Options)]
    private void DirectLateFieldAssignmentSet()
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, (_i0 = _i31)] = _value;
    }

    [MethodImpl(Options)]
    private void InlineMemoryBarrierSet()
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, BarrierIndex()] = _value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int BarrierIndex()
    {
        System.Threading.Thread.MemoryBarrier();
        return _i31;
    }

    [MethodImpl(Options)]
    private void DividingValueSet()
    {
        _array[_i0, _i1, _i2, _i3, _i4, _i5, _i6, _i7, _i8, _i9, _i10, _i11, _i12, _i13, _i14, _i15, _i16, _i17, _i18, _i19, _i20, _i21, _i22, _i23, _i24, _i25, _i26, _i27, _i28, _i29, _i30, _i31] = _value / _divisor;
    }

    private static void VerifyExtraGuards()
    {
        foreach (bool nonzero in new[]
        {
            false,
            true
        }

        )
        {
            if (nonzero && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                continue;
            }

            // Explicit positive shape, in addition to the unchanged copied baseline suite.
            HighRankArrayFieldSinking s = Create(nonzero);
            s.OwnFieldsSet();
            s.ExpectStore(s._originalIndices, 37);
            s.CheckArray("guard positive own fields");
            s = Create(nonzero);
            int[] target = (int[])s._originalIndices.Clone();
            target[31]++;
            s._i31 = target[31];
            if (s._i0 == s._i31)
                throw new Exception("Direct-assignment fixture must change the earlier field.");
            s.DirectLateFieldAssignmentSet();
            Equal(target[31], s._i0, "direct late assignment happened");
            s.ExpectStore(target, 37);
            s.CheckArray("direct late assignment retained earlier index");
            s = Create(nonzero);
            s.InlineMemoryBarrierSet();
            s.ExpectStore(s._originalIndices, 37);
            s.CheckArray("inline memory barrier");
            foreach (bool nil in new[]
            {
                false,
                true
            }

            )
                foreach (bool bad in new[]
                {
                    false,
                    true
                }

                )
                    foreach (bool divideByZero in new[]
                    {
                        false,
                        true
                    }

                    )
                    {
                        s = Create(nonzero);
                        var originalArray = s._array;
                        if (bad)
                            s._i0 = s._array.GetUpperBound(0) + 1;
                        if (nil)
                            s._array = null!;
                        s._divisor = divideByZero ? 0 : 2;
                        Action call = s.DividingValueSet;
                        if (divideByZero)
                            Throws<DivideByZeroException>(call);
                        else if (nil)
                            Throws<NullReferenceException>(call);
                        else if (bad)
                            Throws<IndexOutOfRangeException>(call);
                        else
                            call();
                        s._array = originalArray;
                        if (!nil && !bad && !divideByZero)
                            s.ExpectStore(s._originalIndices, 37 / 2);
                        s.CheckArray($"division precedence nil={nil} bad={bad} zero={divideByZero}");
                    }
        }

        Console.WriteLine("PASS: plain fields, direct late field assignment, aggressively inlined memory barrier, and 16 RHS-division/null/bounds combinations, with zero/nonzero reflection oracles.");
    }
}

public sealed partial class HighRankArrayFieldSinking
{
    [Fact]
    public static void InstanceFieldLoads() => VerifyAll();
    [Fact]
    public static void SideEffectGuards() => VerifyExtraGuards();
}

public sealed partial class HighRankArrayFieldSinking
{
    private struct Holder
    {
        public HighRankArrayFieldSinking Receiver;
        public int Tag;
    }

    private struct IndexHolder
    {
        public int First;
        public int Last;
    }

    // A struct-parent assignment can redefine a promoted REF field under a different local number.
    // Inspect JitDump to determine whether promotion survives; this is also a semantics regression.
    [MethodImpl(Options)]
    private static int PromotedReceiverParentSet(Holder holder, Holder replacement)
    {
        // This ordinary field read establishes that the replacement receiver is non-null.
        _ = replacement.Receiver._value;
        holder.Receiver._array[holder.Receiver._i0, holder.Receiver._i1, holder.Receiver._i2, holder.Receiver._i3, holder.Receiver._i4, holder.Receiver._i5, holder.Receiver._i6, holder.Receiver._i7, holder.Receiver._i8, holder.Receiver._i9, holder.Receiver._i10, holder.Receiver._i11, holder.Receiver._i12, holder.Receiver._i13, holder.Receiver._i14, holder.Receiver._i15, holder.Receiver._i16, holder.Receiver._i17, holder.Receiver._i18, holder.Receiver._i19, holder.Receiver._i20, holder.Receiver._i21, holder.Receiver._i22, holder.Receiver._i23, holder.Receiver._i24, holder.Receiver._i25, holder.Receiver._i26, holder.Receiver._i27, holder.Receiver._i28, holder.Receiver._i29, holder.Receiver._i30, (holder = replacement).Receiver._i31] = holder.Receiver._value;
        return holder.Tag;
    }

    [MethodImpl(Options)]
    private static int PromotedIndexParentSet(HighRankArrayFieldSinking receiver, IndexHolder indices, IndexHolder replacement)
    {
        receiver._array[indices.First, receiver._i1, receiver._i2, receiver._i3, receiver._i4, receiver._i5, receiver._i6, receiver._i7, receiver._i8, receiver._i9, receiver._i10, receiver._i11, receiver._i12, receiver._i13, receiver._i14, receiver._i15, receiver._i16, receiver._i17, receiver._i18, receiver._i19, receiver._i20, receiver._i21, receiver._i22, receiver._i23, receiver._i24, receiver._i25, receiver._i26, receiver._i27, receiver._i28, receiver._i29, receiver._i30, (indices = replacement).Last] = receiver._value;
        return indices.First;
    }

    [Fact]
    public static void PromotedStructParentAssignments()
    {
        foreach (bool nonzero in new[]
        {
            false,
            true
        }

        )
        {
            if (nonzero && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                continue;
            }

            HighRankArrayFieldSinking original = Create(nonzero);
            HighRankArrayFieldSinking replacement = Create(nonzero, 101);
            // Both receiver and earlier indices differ, so reloading either from the replacement
            // must fail the independent whole-array oracle.
            replacement._i0++;
            replacement._i16++;
            replacement._i31++;
            replacement._value = 211;
            Holder holder = new Holder
            {
                Receiver = original,
                Tag = 17
            };
            Holder next = new Holder
            {
                Receiver = replacement,
                Tag = 29
            };
            int[] expectedIndices = (int[])original._originalIndices.Clone();
            expectedIndices[31] = replacement._i31;
            Equal(next.Tag, PromotedReceiverParentSet(holder, next), "promoted receiver parent tag");
            original.ExpectStore(expectedIndices, 211);
            original.CheckArray("promoted receiver parent retains original array and earlier indices");
            replacement.CheckArray("promoted receiver parent replacement array unchanged");
            original = Create(nonzero);
            IndexHolder indices = new IndexHolder
            {
                First = original._i0,
                Last = original._i31
            };
            IndexHolder nextIndices = new IndexHolder
            {
                First = original._i0 + 1,
                Last = original._i31 + 1
            };
            expectedIndices = (int[])original._originalIndices.Clone();
            expectedIndices[31] = nextIndices.Last;
            Equal(nextIndices.First, PromotedIndexParentSet(original, indices, nextIndices), "promoted index parent changed");
            original.ExpectStore(expectedIndices, 37);
            original.CheckArray("promoted index parent retains earlier first index");
            // The replacement field read must throw before an eventual original-array bounds check.
            original = Create(nonzero);
            original._i0 = original._array.GetUpperBound(0) + 1;
            holder = new Holder
            {
                Receiver = original,
                Tag = 17
            };
            next = new Holder
            {
                Receiver = null!,
                Tag = 29
            };
            Throws<NullReferenceException>(() => PromotedReceiverParentSet(holder, next));
            original.CheckArray("null promoted replacement precedes original bounds and does not store");
        }
    }
}
