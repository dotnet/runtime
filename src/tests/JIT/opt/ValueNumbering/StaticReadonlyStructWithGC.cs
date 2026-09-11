// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class StaticReadonlyStructWithGC
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Pre-initialize host type
        RuntimeHelpers.RunClassConstructor(typeof(StaticReadonlyStructWithGC).TypeHandle);

        if (!Test1()) throw new Exception("Test1 failed");
        if (!Test2()) throw new Exception("Test2 failed");
        if (!Test3()) throw new Exception("Test3 failed");
        if (!Test4()) throw new Exception("Test4 failed");
        if (!Test5()) throw new Exception("Test5 failed");
        if (!Test6()) throw new Exception("Test6 failed");
        if (!Test7()) throw new Exception("Test7 failed");
        if (!Test8()) throw new Exception("Test8 failed");
        if (!Test9()) throw new Exception("Test9 failed");

        RuntimeHelpers.RunClassConstructor(typeof(MovableRefHolder).TypeHandle);

        // Bake the values in (if the JIT incorrectly folds them, this is where it happens),
        // then relocate the referenced object with a compacting gen2 GC and read again.
        ReadLowHalfOptimized();
        ReadAcrossSlotOptimized();

        for (int i = 0; i < 4; i++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();
        }

        if (ReadLowHalfOptimized() != ReadLowHalfUnoptimized())
            throw new Exception("Test10 failed");
        if (ReadAcrossSlotOptimized() != ReadAcrossSlotUnoptimized())
            throw new Exception("Test11 failed");

        GC.KeepAlive(MovableRefHolder.Value.Obj);
    }

    static readonly MyStruct MyStructFld = new()
    {
        A = "A",
        B = 111111.ToString(), // non-literal
        C = new MyStruct2 { A = "AA" },
        D = typeof(int),
        E = () => 42,
        F = new MyStruct3 { A = typeof(double), B = typeof(string) },
        G = new int[0],
        H = null
    };

    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test1() => MyStructFld.A == "A";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test2() => MyStructFld.B == "111111";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test3() => MyStructFld.C.A == "AA";
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test4() => MyStructFld.D == typeof(int);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test5() => MyStructFld.E() == 42;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test6() => MyStructFld.F.A == typeof(double);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test7() => MyStructFld.F.B == typeof(string);
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test8() => MyStructFld.G.Length == 0;
    [MethodImpl(MethodImplOptions.NoInlining)] static bool Test9() => MyStructFld.H == null;

    // The JIT is allowed to fold a whole object reference slot of a static readonly struct into
    // a constant (the referenced object then has to be non-movable), but it must never fold a
    // read that only partially overlaps such a slot - the GC is free to relocate the object and
    // rewrite the pointer, which makes the baked bytes stale.

    struct Holder
    {
        public object Obj;
        public int Tag;
        public int Tag2;
    }

    static class MovableRefHolder
    {
        internal static readonly Holder Value;

        static MovableRefHolder()
        {
            // Allocate garbage in front of the target object so that a compacting gen2 GC
            // actually has a reason to move it.
            object[] filler = new object[100000];
            for (int i = 0; i < filler.Length; i++)
            {
                filler[i] = new object();
            }
            Value = new Holder { Obj = new object(), Tag = 0x12345678, Tag2 = unchecked((int)0x9ABCDEF0) };
            GC.KeepAlive(filler);
        }
    }

    // Reads the low half of Holder.Obj.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int ReadLowHalfOptimized() =>
        Unsafe.As<Holder, int>(ref Unsafe.AsRef(in MovableRefHolder.Value));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static int ReadLowHalfUnoptimized() =>
        Unsafe.As<Holder, int>(ref Unsafe.AsRef(in MovableRefHolder.Value));

    // Reads the high half of Holder.Obj together with Holder.Tag (on 32-bit targets this read
    // does not overlap the object reference slot, it just covers Tag and Tag2).
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static long ReadAcrossSlotOptimized() =>
        Unsafe.ReadUnaligned<long>(
            ref Unsafe.Add(ref Unsafe.As<Holder, byte>(ref Unsafe.AsRef(in MovableRefHolder.Value)), 4));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    static long ReadAcrossSlotUnoptimized() =>
        Unsafe.ReadUnaligned<long>(
            ref Unsafe.Add(ref Unsafe.As<Holder, byte>(ref Unsafe.AsRef(in MovableRefHolder.Value)), 4));

    struct MyStruct
    {
        public string A;
        public string B;
        public MyStruct2 C;
        public Type D;
        public Func<int> E;
        public MyStruct3 F;
        public int[] G;
        public object H;
    }

    struct MyStruct2
    {
        public string A;
    }

    struct MyStruct3
    {
        public Type A;
        public Type B;
    }
}
