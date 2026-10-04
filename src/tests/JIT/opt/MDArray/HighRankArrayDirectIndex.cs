// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public sealed class HighRankArrayDirectIndex
{
    private const MethodImplOptions Opt = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;
    private int[,,,] _a;
    private int _i0, _i1, _i2, _i3, _delta = 1, _divisor = 1, _trace;
    private volatile int _volatileIndex;
    private HighRankArrayDirectIndex _other;
    private readonly int _lo;
    public HighRankArrayDirectIndex() : this(0)
    {
    }

    private HighRankArrayDirectIndex(int lo)
    {
        _lo = lo;
        _i0 = _i1 = _i2 = _i3 = _volatileIndex = lo;
        _a = (int[,,,])Array.CreateInstance(typeof(int), new[] { 2, 2, 2, 2 }, new[] { lo, lo, lo, lo });
        for (int a = 0; a < 2; a++)
            for (int b = 0; b < 2; b++)
                for (int c = 0; c < 2; c++)
                    for (int d = 0; d < 2; d++)
                        ((Array)_a).SetValue(1000 * a + 100 * b + 10 * c + d + 7, lo + a, lo + b, lo + c, lo + d);
        _other = this;
    }

    [MethodImpl(Opt)]
    private int ChangeFirst()
    {
        _i0++;
        _trace++;
        return _i1;
    }

    [MethodImpl(Opt)]
    private int ThrowLate()
    {
        _trace++;
        if (_divisor == 0)
            throw new InvalidOperationException();
        return _i3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Barrier()
    {
        Thread.MemoryBarrier();
        return _i1;
    }

    [MethodImpl(Opt)]
    private int GetOwn() => _a[_i0, _i1, _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressOwn() => ref _a[_i0, _i1, _i2, _i3];
    [MethodImpl(Opt)]
    private int GetAdd() => _a[_i0, _i1, _i2, unchecked(_i3 + 1)];
    [MethodImpl(Opt)]
    private ref int AddressAdd() => ref _a[_i0, _i1, _i2, unchecked(_i3 + 1)];
    [MethodImpl(Opt)]
    private int GetDifferent() => _a[_other._i0, _i1, _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressDifferent() => ref _a[_other._i0, _i1, _i2, _i3];
    [MethodImpl(Opt)]
    private int GetVolatile() => _a[_i0, _volatileIndex, _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressVolatile() => ref _a[_i0, _volatileIndex, _i2, _i3];
    [MethodImpl(Opt)]
    private int GetMutation() => _a[_i0, ChangeFirst(), _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressMutation() => ref _a[_i0, ChangeFirst(), _i2, _i3];
    [MethodImpl(Opt)]
    private int GetDirectStore() => _a[_i0, (_i0 = _i1 + 1), _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressDirectStore() => ref _a[_i0, (_i0 = _i1 + 1), _i2, _i3];
    [MethodImpl(Opt)]
    private int GetReassign(HighRankArrayDirectIndex other) => _a[_other._i0, (_other = other)._i1, _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressReassign(HighRankArrayDirectIndex other) => ref _a[_other._i0, (_other = other)._i1, _i2, _i3];
    [MethodImpl(Opt)]
    private int GetChecked() => _a[_i0, _i1, _i2, checked(_i3 + _delta)];
    [MethodImpl(Opt)]
    private ref int AddressChecked() => ref _a[_i0, _i1, _i2, checked(_i3 + _delta)];
    [MethodImpl(Opt)]
    private int GetDivide() => _a[_i0, _i1, _i2, _i3 / _divisor];
    [MethodImpl(Opt)]
    private ref int AddressDivide() => ref _a[_i0, _i1, _i2, _i3 / _divisor];
    [MethodImpl(Opt)]
    private int GetLateThrow() => _a[_i0, _i1, _i2, ThrowLate()];
    [MethodImpl(Opt)]
    private ref int AddressLateThrow() => ref _a[_i0, _i1, _i2, ThrowLate()];
    [MethodImpl(Opt)]
    private int GetBarrier() => _a[_i0, Barrier(), _i2, _i3];
    [MethodImpl(Opt)]
    private ref int AddressBarrier() => ref _a[_i0, Barrier(), _i2, _i3];
    [MethodImpl(Opt)]
    private int GetNested() => _a[_i0, _i1, _i2, _a[_i0, _i1, _i2, _i3]];
    [MethodImpl(Opt)]
    private ref int AddressNested() => ref _a[_i0, _i1, _i2, _a[_i0, _i1, _i2, _i3]];
    [MethodImpl(Opt)]
    private int Siblings() => _a[_i0, _i1, _i2, _i3] + _a[_i0, _i1, _i2, _i3];
    private int Read(int a = 0, int b = 0, int c = 0, int d = 0) => (int)((Array)_a).GetValue(_lo + a, _lo + b, _lo + c, _lo + d);
    private static void Expect<T>(Action action)
        where T : Exception
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Assert.Equal(typeof(T), e.GetType());
            return;
        }

        throw new Exception("Missing expected " + typeof(T).Name);
    }

    [Fact]
    public static void DirectExpressionSemantics()
    {
        foreach (int lo in new[]
        {
            0,
            -2
        }

        )
        {
            if (lo != 0 && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
                continue;
            var x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetOwn());
            ref int slot = ref x.AddressOwn();
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            slot = 901;
            Assert.Equal(901, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(d: 1), x.GetAdd());
            x.AddressAdd() = 902;
            Assert.Equal(902, x.Read(d: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetDifferent());
            x.AddressDifferent() = 903;
            Assert.Equal(903, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetVolatile());
            x.AddressVolatile() = 904;
            Assert.Equal(904, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetMutation());
            Assert.Equal(lo + 1, x._i0);
            Assert.Equal(1, x._trace);
            x = new HighRankArrayDirectIndex(lo);
            x.AddressMutation() = 905;
            Assert.Equal(905, x.Read());
            Assert.Equal(1007, x.Read(a: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(b: 1), x.GetDirectStore());
            Assert.Equal(lo + 1, x._i0);
            x = new HighRankArrayDirectIndex(lo);
            x.AddressDirectStore() = 906;
            Assert.Equal(906, x.Read(b: 1));
            Assert.Equal(1107, x.Read(a: 1, b: 1));
            var other = new HighRankArrayDirectIndex(lo);
            other._i0++;
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetReassign(other));
            Assert.Same(other, x._other);
            x = new HighRankArrayDirectIndex(lo);
            x.AddressReassign(other) = 907;
            Assert.Equal(907, x.Read());
            Assert.Same(other, x._other);
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(d: 1), x.GetChecked());
            x.AddressChecked() = 908;
            Assert.Equal(908, x.Read(d: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetDivide());
            x.AddressDivide() = 909;
            Assert.Equal(909, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetLateThrow());
            x.AddressLateThrow() = 910;
            Assert.Equal(910, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), x.GetBarrier());
            x.AddressBarrier() = 911;
            Assert.Equal(911, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            ((Array)x._a).SetValue(lo, lo, lo, lo, lo);
            Assert.Equal(lo, x.GetNested());
            x.AddressNested() = 912;
            Assert.Equal(912, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(14, x.Siblings());
            // Later index exceptions must win over null or first-dimension bounds failure.
            foreach (bool nil in new[]
            {
                false,
                true
            }

            )
            {
                x = new HighRankArrayDirectIndex(lo);
                x._i0 = lo + 2;
                if (nil)
                    x._a = null;
                x._i3 = int.MaxValue;
                Expect<OverflowException>(() => x.GetChecked());
                Expect<OverflowException>(() =>
                {
                    x.AddressChecked() = 1;
                });
                x._divisor = 0;
                Expect<DivideByZeroException>(() => x.GetDivide());
                Expect<DivideByZeroException>(() =>
                {
                    x.AddressDivide() = 1;
                });
                Expect<InvalidOperationException>(() => x.GetLateThrow());
                Expect<InvalidOperationException>(() =>
                {
                    x.AddressLateThrow() = 1;
                });
                x._other = null;
                Expect<NullReferenceException>(() => x.GetDifferent());
                Expect<NullReferenceException>(() =>
                {
                    x.AddressDifferent() = 1;
                });
            }

            x = new HighRankArrayDirectIndex(lo);
            x._i0 = lo + 2;
            Expect<IndexOutOfRangeException>(() => x.GetOwn());
            Expect<IndexOutOfRangeException>(() =>
            {
                x.AddressOwn() = 1;
            });
            x = new HighRankArrayDirectIndex(lo);
            x._a = null;
            Expect<NullReferenceException>(() => x.GetOwn());
            Expect<NullReferenceException>(() =>
            {
                x.AddressOwn() = 1;
            });
        }
    }

    // Explicit receiver checks and a local array operand reach the index-group proof.
    [MethodImpl(Opt)]
    private static int GuardedGetOwn(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressOwn(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetAdd(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, unchecked(receiver._i3 + 1)];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressAdd(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, unchecked(receiver._i3 + 1)];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetDifferent(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._other._i0, receiver._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressDifferent(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._other._i0, receiver._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetVolatile(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._volatileIndex, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressVolatile(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._volatileIndex, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetMutation(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver.ChangeFirst(), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressMutation(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver.ChangeFirst(), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetDirectStore(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, (receiver._i0 = receiver._i1 + 1), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressDirectStore(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, (receiver._i0 = receiver._i1 + 1), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetReassign(int[,,,] array, HighRankArrayDirectIndex receiver, HighRankArrayDirectIndex other)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._other._i0, (receiver._other = other)._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressReassign(int[,,,] array, HighRankArrayDirectIndex receiver, HighRankArrayDirectIndex other)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._other._i0, (receiver._other = other)._i1, receiver._i2, receiver._i3];
    }

    // The first index must still use the original receiver after the local is reassigned.
    // The remaining field reads can reach direct deferral after importer captures.
    [MethodImpl(Opt)]
    private static int GuardedGetLocalReassign(int[,,,] array, HighRankArrayDirectIndex receiver, HighRankArrayDirectIndex other)
    {
        if (receiver is null || other is null)
            throw new NullReferenceException();
        return array[receiver._i0, (receiver = other)._i1, receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressLocalReassign(int[,,,] array, HighRankArrayDirectIndex receiver, HighRankArrayDirectIndex other)
    {
        if (receiver is null || other is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, (receiver = other)._i1, receiver._i2, receiver._i3];
    }

    // No managed array or holder reference escapes this frame; the caller keeps only
    // the interior byref produced by the direct-deferral accessor across compacting GC.
    [MethodImpl(Opt)]
    private static ref int CreateDetachedSlot(int lo)
    {
        var receiver = new HighRankArrayDirectIndex(lo);
        return ref GuardedAddressOwn(receiver._a, receiver);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ForceCompactingGC()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    [MethodImpl(Opt)]
    private static int GuardedGetChecked(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, checked(receiver._i3 + receiver._delta)];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressChecked(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, checked(receiver._i3 + receiver._delta)];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetDivide(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, receiver._i3 / receiver._divisor];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressDivide(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, receiver._i3 / receiver._divisor];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetLateThrow(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, receiver.ThrowLate()];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressLateThrow(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, receiver.ThrowLate()];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetBarrier(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver.Barrier(), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressBarrier(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver.Barrier(), receiver._i2, receiver._i3];
    }

    [MethodImpl(Opt)]
    private static int GuardedGetNested(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return array[receiver._i0, receiver._i1, receiver._i2, array[receiver._i0, receiver._i1, receiver._i2, receiver._i3]];
    }

    [MethodImpl(Opt)]
    private static ref int GuardedAddressNested(int[,,,] array, HighRankArrayDirectIndex receiver)
    {
        if (receiver is null)
            throw new NullReferenceException();
        return ref array[receiver._i0, receiver._i1, receiver._i2, array[receiver._i0, receiver._i1, receiver._i2, receiver._i3]];
    }

    [Fact]
    public static void GuardedDirectExpressionSemantics()
    {
        Expect<NullReferenceException>(() => GuardedGetOwn(null, null));
        Expect<NullReferenceException>(() =>
        {
            GuardedAddressOwn(null, null) = 1;
        });
        foreach (int lo in new[]
        {
            0,
            -2
        }

        )
        {
            if (lo != 0 && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
                continue;
            var x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetOwn(x._a, x));
            ref int slot = ref GuardedAddressOwn(x._a, x);
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            slot = 901;
            Assert.Equal(901, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(d: 1), GuardedGetAdd(x._a, x));
            GuardedAddressAdd(x._a, x) = 902;
            Assert.Equal(902, x.Read(d: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetDifferent(x._a, x));
            GuardedAddressDifferent(x._a, x) = 903;
            Assert.Equal(903, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetVolatile(x._a, x));
            GuardedAddressVolatile(x._a, x) = 904;
            Assert.Equal(904, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetMutation(x._a, x));
            Assert.Equal(lo + 1, x._i0);
            Assert.Equal(1, x._trace);
            x = new HighRankArrayDirectIndex(lo);
            GuardedAddressMutation(x._a, x) = 905;
            Assert.Equal(905, x.Read());
            Assert.Equal(1007, x.Read(a: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(b: 1), GuardedGetDirectStore(x._a, x));
            Assert.Equal(lo + 1, x._i0);
            x = new HighRankArrayDirectIndex(lo);
            GuardedAddressDirectStore(x._a, x) = 906;
            Assert.Equal(906, x.Read(b: 1));
            Assert.Equal(1107, x.Read(a: 1, b: 1));
            var other = new HighRankArrayDirectIndex(lo);
            other._i0++;
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetReassign(x._a, x, other));
            Assert.Same(other, x._other);
            x = new HighRankArrayDirectIndex(lo);
            GuardedAddressReassign(x._a, x, other) = 907;
            Assert.Equal(907, x.Read());
            Assert.Same(other, x._other);
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(d: 1), GuardedGetChecked(x._a, x));
            GuardedAddressChecked(x._a, x) = 908;
            Assert.Equal(908, x.Read(d: 1));
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetDivide(x._a, x));
            GuardedAddressDivide(x._a, x) = 909;
            Assert.Equal(909, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetLateThrow(x._a, x));
            GuardedAddressLateThrow(x._a, x) = 910;
            Assert.Equal(910, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(x.Read(), GuardedGetBarrier(x._a, x));
            GuardedAddressBarrier(x._a, x) = 911;
            Assert.Equal(911, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            ((Array)x._a).SetValue(lo, lo, lo, lo, lo);
            Assert.Equal(lo, GuardedGetNested(x._a, x));
            GuardedAddressNested(x._a, x) = 912;
            Assert.Equal(912, x.Read());
            x = new HighRankArrayDirectIndex(lo);
            Assert.Equal(14, x.Siblings());
            // Later index exceptions must win over null or first-dimension bounds failure.
            foreach (bool nil in new[]
            {
                false,
                true
            }

            )
            {
                x = new HighRankArrayDirectIndex(lo);
                x._i0 = lo + 2;
                if (nil)
                    x._a = null;
                x._i3 = int.MaxValue;
                Expect<OverflowException>(() => GuardedGetChecked(x._a, x));
                Expect<OverflowException>(() =>
                {
                    GuardedAddressChecked(x._a, x) = 1;
                });
                x._divisor = 0;
                Expect<DivideByZeroException>(() => GuardedGetDivide(x._a, x));
                Expect<DivideByZeroException>(() =>
                {
                    GuardedAddressDivide(x._a, x) = 1;
                });
                Expect<InvalidOperationException>(() => GuardedGetLateThrow(x._a, x));
                Expect<InvalidOperationException>(() =>
                {
                    GuardedAddressLateThrow(x._a, x) = 1;
                });
                x._other = null;
                Expect<NullReferenceException>(() => GuardedGetDifferent(x._a, x));
                Expect<NullReferenceException>(() =>
                {
                    GuardedAddressDifferent(x._a, x) = 1;
                });
            }

            x = new HighRankArrayDirectIndex(lo);
            x._i0 = lo + 2;
            Expect<IndexOutOfRangeException>(() => GuardedGetOwn(x._a, x));
            Expect<IndexOutOfRangeException>(() =>
            {
                GuardedAddressOwn(x._a, x) = 1;
            });
            x = new HighRankArrayDirectIndex(lo);
            x._a = null;
            Expect<NullReferenceException>(() => GuardedGetOwn(x._a, x));
            Expect<NullReferenceException>(() =>
            {
                GuardedAddressOwn(x._a, x) = 1;
            });
        }
    }

    [Fact]
    public static void DirectDeferralReassignmentAndDetachedByref()
    {
        foreach (int lo in new[]
        {
            0,
            -2
        }

        )
        {
            if (lo != 0 && !TestLibrary.PlatformDetection.IsNonZeroLowerBoundArraySupported)
                continue;
            var receiver = new HighRankArrayDirectIndex(lo);
            var other = new HighRankArrayDirectIndex(lo);
            other._i0++;
            other._i1++;
            other._i2++;
            other._i3++;
            Assert.Equal(receiver.Read(b: 1, c: 1, d: 1), GuardedGetLocalReassign(receiver._a, receiver, other));
            GuardedAddressLocalReassign(receiver._a, receiver, other) = 913;
            Assert.Equal(913, receiver.Read(b: 1, c: 1, d: 1));
            Assert.Equal(1118, receiver.Read(a: 1, b: 1, c: 1, d: 1));
            ref int slot = ref CreateDetachedSlot(lo);
            ForceCompactingGC();
            Assert.Equal(7, slot);
            slot = 914;
            ForceCompactingGC();
            Assert.Equal(914, slot);
        }
    }
}
