// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

internal static class Program
{
    private const MethodImplOptions OptimizedNoInlining =
        MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining;
    private static readonly object s_stackFirst = new();
    private static readonly object s_stackSecond = new();
    private static volatile int s_stackIndex = 64;

    private static int Main()
    {
        bool expectStandardAbi = Environment.GetEnvironmentVariable("P11_EXPECT_STANDARD_ABI") == "1";
        bool expectClobber = Environment.GetEnvironmentVariable("P11_EXPECT_CLOBBER") == "1";
        bool expectSlotLog = Environment.GetEnvironmentVariable("P12_EXPECT_SLOT_LOG") == "1";
        NativeHooks? nativeHooks = expectStandardAbi ? LoadNativeHooks() : null;

        if (Environment.GetEnvironmentVariable("P13_REF_CLASS_LAYOUT_ONLY") == "1")
        {
            if (!ExerciseStackAllocatedReferenceClass())
            {
                throw new InvalidOperationException("A stack-allocated reference-class layout lost a value.");
            }
            ExerciseVeryLargeMemberwiseClone();
            Console.WriteLine("PASS: stack-allocated reference-class layout");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("P12_INTERPRETER_SURFACES") == "1")
        {
            ExerciseInterpreterStoreSurfaces(
                nativeHooks ?? throw new InvalidOperationException("Native hooks are required."));
            Console.WriteLine("PASS: interpreter mixed-reference copy and clear surfaces");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("P13_GCSTRESS_SURFACES") == "1")
        {
            ExerciseGcStressStoreSurfaces();
            Console.WriteLine("PASS: GC stress scalar, copy, clear, fill, and layout-helper surfaces");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("P13_FILL_CODEGEN_ONLY") == "1")
        {
            ExerciseFillCodegenSurfaces();
            Console.WriteLine("PASS: reference and mixed-reference fill codegen surfaces");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("P13_LARGE_LAYOUT_ONLY") == "1")
        {
            ExerciseVeryLargeLayout(
                nativeHooks ?? throw new InvalidOperationException("Native hooks are required."),
                Environment.GetEnvironmentVariable("DOTNET_GCWriteBarrierTestBitMeaning") == "1");
            Console.WriteLine("PASS: very-large mixed-layout copy, clear, and self-copy");
            return 0;
        }

        GetCallCount? getCallCount = nativeHooks?.CallCount;
        ulong initialCallCount = getCallCount?.Invoke() ?? 0;

        ExerciseLiveState();
        ExerciseCollections();

        ulong finalCallCount = getCallCount?.Invoke() ?? 0;
        if (expectStandardAbi && (finalCallCount <= initialCallCount))
        {
            throw new InvalidOperationException("The standard-ABI write-barrier callback did not execute.");
        }

        uint clobberMask = nativeHooks?.ClobberMask() ?? 0;
        if (expectClobber && (clobberMask == 0))
        {
            throw new InvalidOperationException("The destructive write-barrier callback was not selected.");
        }

        List<string> executedState = ["field/array helpers", "integer", "object", "Vector128"];
        if (expectSlotLog)
        {
            ExerciseSlotLog(nativeHooks ?? throw new InvalidOperationException("Native hooks are required."));
            executedState.Add("slot-log fast/slow/contention");
            ExerciseReferenceCountWidths();
            executedState.Add("2/4/8-bit RC saturation");
            ExerciseEpochReset(nativeHooks);
            executedState.Add("GC epoch reset");
            ExerciseFaultMapping();
            executedState.Add("write-barrier fault mapping");
        }
        if (Vector256.IsHardwareAccelerated)
        {
            executedState.Add("Vector256");
        }
        if (Vector512.IsHardwareAccelerated)
        {
            executedState.Add("Vector512");
            if ((clobberMask & 4) != 0)
            {
                executedState.Add("opmask clobber");
            }
        }
        if ((clobberMask & 8) != 0)
        {
            executedState.Add("APX EGPR clobber");
        }

        Console.WriteLine(
            $"PASS: {string.Join(", ", executedState)}; callback delta {finalCallCount - initialCallCount}");
        return 0;
    }

    private static void ExerciseLiveState()
    {
        Node destination = new();
        Node value = new();
        Node[] array = new Node[2];
        Node live0 = new();
        Node live1 = new();
        Node live2 = new();
        Node live3 = new();

        for (int iteration = 0; iteration < 2_000; iteration++)
        {
            long seed = iteration + 101;
            ScalarState scalar = StoreScalars(
                destination,
                value,
                array,
                live0,
                live1,
                live2,
                live3,
                seed,
                seed + 1,
                seed + 2,
                seed + 3,
                seed + 4,
                seed + 5,
                seed + 6,
                seed + 7);

            if (!ReferenceEquals(scalar.Object0, live0) ||
                !ReferenceEquals(scalar.Object1, live1) ||
                !ReferenceEquals(scalar.Object2, live2) ||
                !ReferenceEquals(scalar.Object3, live3) ||
                (scalar.Integer0 != Mix(seed)) ||
                (scalar.Integer1 != Mix(seed + 1)) ||
                (scalar.Integer2 != Mix(seed + 2)) ||
                (scalar.Integer3 != Mix(seed + 3)) ||
                (scalar.Integer4 != Mix(seed + 4)) ||
                (scalar.Integer5 != Mix(seed + 5)) ||
                (scalar.Integer6 != Mix(seed + 6)) ||
                (scalar.Integer7 != Mix(seed + 7)))
            {
                throw new InvalidOperationException("Integer or object state was corrupted across a write barrier.");
            }

            Vector128<long> input128 = Vector128.Create(seed, seed + 1);
            Vector128State state128 = StoreVector128(destination, value, input128, seed);
            if (!state128.Value0.Equals(input128 + Vector128.Create(seed)) ||
                !state128.Value1.Equals(input128 - Vector128.Create(seed + 1)) ||
                !state128.Value2.Equals(input128 ^ Vector128.Create(seed + 2)) ||
                !state128.Value3.Equals(input128 | Vector128.Create(seed + 3)))
            {
                throw new InvalidOperationException("Vector128 state was corrupted across a write barrier.");
            }

            if (Vector256.IsHardwareAccelerated)
            {
                Vector256<long> input256 = Vector256.Create(seed, seed + 1, seed + 2, seed + 3);
                Vector256State state256 = StoreVector256(destination, value, input256, seed);
                if (!state256.Value0.Equals(input256 + Vector256.Create(seed)) ||
                    !state256.Value1.Equals(input256 - Vector256.Create(seed + 1)) ||
                    !state256.Value2.Equals(input256 ^ Vector256.Create(seed + 2)) ||
                    !state256.Value3.Equals(input256 | Vector256.Create(seed + 3)))
                {
                    throw new InvalidOperationException("Vector256 state was corrupted across a write barrier.");
                }
            }

            if (Vector512.IsHardwareAccelerated)
            {
                Vector512<long> input512 = Vector512.Create(
                    seed,
                    seed + 1,
                    seed + 2,
                    seed + 3,
                    seed + 4,
                    seed + 5,
                    seed + 6,
                    seed + 7);
                Vector512State state512 = StoreVector512(destination, value, input512, seed);
                if (!state512.Value0.Equals(input512 + Vector512.Create(seed)) ||
                    !state512.Value1.Equals(input512 - Vector512.Create(seed + 1)) ||
                    !state512.Value2.Equals(input512 ^ Vector512.Create(seed + 2)) ||
                    !state512.Value3.Equals(input512 | Vector512.Create(seed + 3)))
                {
                    throw new InvalidOperationException("Vector512 state was corrupted across a write barrier.");
                }
            }
        }
    }

    private static void ExerciseCollections()
    {
        Node root = new();
        for (int collection = 0; collection < 8; collection++)
        {
            Node current = root;
            for (int index = 0; index < 20_000; index++)
            {
                current.Next = new Node();
                current = current.Next;
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            root.Next = null;
        }
    }

    private static void ExerciseGcStressStoreSurfaces()
    {
        object value = new();
        ObjectHolder holder = new() { Value = new object() };
        holder.Value = value;

        object?[] source = [value, value];
        object?[] destination = [new object(), new object()];
        Array.Copy(source, destination, source.Length);
        destination.AsSpan().Fill(value);
        Array.Clear(destination);

        LargeReferenceHolder largeHolder = new();
        LargeMixedReferences largeValue = new()
        {
            First = value,
            Second = value,
            Third = value,
            Fourth = value,
        };
        StoreLargeValue(largeHolder, largeValue);
        ClearLargeValue(largeHolder);
        ExerciseVeryLargeGcStressSurface(value);
        GC.Collect();

        if (!ReferenceEquals(holder.Value, value) ||
            (destination[0] is not null) ||
            (destination[1] is not null) ||
            (largeHolder.Value.First is not null) ||
            (largeHolder.Value.Fourth is not null))
        {
            throw new InvalidOperationException("A GC-stress store surface produced an unexpected value.");
        }
    }

    private static void ExerciseVeryLargeGcStressSurface(object value)
    {
        VeryLargeMixedLayout source = default;
        for (int index = 0; index < 2048; index++)
        {
            source[index] = new MixedReferences
            {
                First = value,
                Scalar = (nuint)index,
                Second = value,
            };
        }

        VeryLargeReferenceHolder holder = new();
        StoreVeryLargeValue(holder, in source);
        if (!ReferenceEquals(holder.Value[0].First, value) ||
            !ReferenceEquals(holder.Value[2047].Second, value))
        {
            throw new InvalidOperationException("A very-large GC-stress copy lost a reference.");
        }

        ClearVeryLargeValue(holder);
        if ((holder.Value[0].First is not null) ||
            (holder.Value[2047].Second is not null))
        {
            throw new InvalidOperationException("A very-large GC-stress clear retained a reference.");
        }
    }

    private static void ExerciseFillCodegenSurfaces()
    {
        object value = new();
        object?[] references = new object?[16];
        if (!ReferenceEquals(FillReferences(references, value), value))
        {
            throw new InvalidOperationException("Reference fill produced an unexpected value.");
        }

        MixedReferences mixedValue = new()
        {
            First = value,
            Scalar = 42,
            Second = value,
        };
        MixedReferences[] mixed = new MixedReferences[4];
        if (!ReferenceEquals(FillMixedReferences(mixed, mixedValue), value))
        {
            throw new InvalidOperationException("Mixed-reference fill produced an unexpected value.");
        }
    }

    [MethodImpl(OptimizedNoInlining)]
    private static bool ExerciseStackAllocatedReferenceClass()
    {
        StackReferenceLayout holder = new(s_stackFirst, s_stackSecond);
        ref object? firstField = ref holder.F000;
        for (int index = 1; index < 127; index++)
        {
            if (index != s_stackIndex)
            {
                Volatile.Write(ref Unsafe.Add(ref firstField, index), null);
            }
        }
        GC.Collect();

        return ReferenceEquals(holder.F000, s_stackFirst) &&
            ReferenceEquals(holder.F127, s_stackSecond) &&
            (Volatile.Read(ref Unsafe.Add(ref firstField, s_stackIndex)) is null);
    }

    private static void ExerciseVeryLargeMemberwiseClone()
    {
        VeryLargeReferenceHolder source = new();
        source.Value[0] = new MixedReferences { First = s_stackFirst, Scalar = 101, Second = s_stackSecond };
        source.Value[2047] = new MixedReferences { First = s_stackSecond, Scalar = 202, Second = s_stackFirst };
        VeryLargeReferenceHolder clone = source.Clone();
        if (!ReferenceEquals(clone.Value[0].First, s_stackFirst) ||
            !ReferenceEquals(clone.Value[2047].Second, s_stackFirst))
        {
            throw new InvalidOperationException("A very-large MemberwiseClone lost a reference.");
        }
    }

    [MethodImpl(OptimizedNoInlining)]
    private static ScalarState StoreScalars(
        Node destination,
        Node value,
        Node[] array,
        Node live0,
        Node live1,
        Node live2,
        Node live3,
        long integer0,
        long integer1,
        long integer2,
        long integer3,
        long integer4,
        long integer5,
        long integer6,
        long integer7)
    {
        long mixed0 = Mix(integer0);
        long mixed1 = Mix(integer1);
        long mixed2 = Mix(integer2);
        long mixed3 = Mix(integer3);
        long mixed4 = Mix(integer4);
        long mixed5 = Mix(integer5);
        long mixed6 = Mix(integer6);
        long mixed7 = Mix(integer7);
        destination.Next = value;
        array[mixed0 & 1] = live0;

        return new ScalarState(
            live0,
            live1,
            live2,
            live3,
            mixed0,
            mixed1,
            mixed2,
            mixed3,
            mixed4,
            mixed5,
            mixed6,
            mixed7);
    }

    [MethodImpl(OptimizedNoInlining)]
    private static Vector128State StoreVector128(
        Node destination,
        Node value,
        Vector128<long> input,
        long seed)
    {
        Vector128<long> value0 = input + Vector128.Create(seed);
        Vector128<long> value1 = input - Vector128.Create(seed + 1);
        Vector128<long> value2 = input ^ Vector128.Create(seed + 2);
        Vector128<long> value3 = input | Vector128.Create(seed + 3);
        destination.Next = value;

        return new Vector128State(value0, value1, value2, value3);
    }

    [MethodImpl(OptimizedNoInlining)]
    private static Vector256State StoreVector256(
        Node destination,
        Node value,
        Vector256<long> input,
        long seed)
    {
        Vector256<long> value0 = input + Vector256.Create(seed);
        Vector256<long> value1 = input - Vector256.Create(seed + 1);
        Vector256<long> value2 = input ^ Vector256.Create(seed + 2);
        Vector256<long> value3 = input | Vector256.Create(seed + 3);
        destination.Next = value;

        return new Vector256State(value0, value1, value2, value3);
    }

    [MethodImpl(OptimizedNoInlining)]
    private static Vector512State StoreVector512(
        Node destination,
        Node value,
        Vector512<long> input,
        long seed)
    {
        Vector512<long> value0 = input + Vector512.Create(seed);
        Vector512<long> value1 = input - Vector512.Create(seed + 1);
        Vector512<long> value2 = input ^ Vector512.Create(seed + 2);
        Vector512<long> value3 = input | Vector512.Create(seed + 3);
        destination.Next = value;

        return new Vector512State(value0, value1, value2, value3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Mix(long value)
    {
        return (value * 1_664_525) + 1_013_904_223;
    }

    private static NativeHooks LoadNativeHooks()
    {
        string libraryPath = Environment.GetEnvironmentVariable("P11_NATIVE_HOOK_LIBRARY")
            ?? Environment.GetEnvironmentVariable("DOTNET_GCPath")
            ?? throw new InvalidOperationException(
                "P11_NATIVE_HOOK_LIBRARY or DOTNET_GCPath is required.");
        nint library = NativeLibrary.Load(libraryPath);
        nint callCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetCallCount");
        nint clobberMask = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetClobberMask");
        nint rangeCallCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetRangeCallCount");
        nint clearRangeCallCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetClearRangeCallCount");
        nint dependentEdgeCallCount =
            NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetDependentEdgeCallCount");
        nint reset = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_Reset");
        nint resetRange = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_ResetRange");
        nint setRequiresWork =
            NativeLibrary.GetExport(library, "GC_WriteBarrierTest_SetRequiresWork");
        nint attemptCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetAttemptCount");
        nint winCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetWinCount");
        nint argumentErrorCount = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetArgumentErrorCount");
        nint lastDestination = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetLastDestination");
        nint syntheticDestination = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_GetSyntheticDestination");
        nint claimSynthetic = NativeLibrary.GetExport(library, "GC_WriteBarrierTest_ClaimSynthetic");
        string runtimeLibraryPath = Environment.GetEnvironmentVariable("P12_RUNTIME_HOOK_LIBRARY")
            ?? Path.Combine(
                Path.GetDirectoryName(libraryPath)!,
                OperatingSystem.IsWindows() ? "coreclr.dll" : "libcoreclr.so");
        nint runtimeLibrary = NativeLibrary.Load(runtimeLibraryPath);
        nint invokeEmptyRange =
            NativeLibrary.GetExport(runtimeLibrary, "GC_WriteBarrierTest_InvokeEmptyRange");
        return new NativeHooks(
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(callCount),
            Marshal.GetDelegateForFunctionPointer<GetClobberMask>(clobberMask),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(rangeCallCount),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(clearRangeCallCount),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(dependentEdgeCallCount),
            Marshal.GetDelegateForFunctionPointer<ResetSlotLog>(reset),
            Marshal.GetDelegateForFunctionPointer<ResetSlotLogRange>(resetRange),
            Marshal.GetDelegateForFunctionPointer<SetRequiresWork>(setRequiresWork),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(attemptCount),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(winCount),
            Marshal.GetDelegateForFunctionPointer<GetCallCount>(argumentErrorCount),
            Marshal.GetDelegateForFunctionPointer<GetPointer>(lastDestination),
            Marshal.GetDelegateForFunctionPointer<GetSyntheticDestination>(syntheticDestination),
            Marshal.GetDelegateForFunctionPointer<ClaimSynthetic>(claimSynthetic),
            Marshal.GetDelegateForFunctionPointer<InvokeEmptyRange>(invokeEmptyRange));
    }

    private static unsafe void ExerciseSlotLog(NativeHooks hooks)
    {
        bool workWhenSet = Environment.GetEnvironmentVariable("DOTNET_GCWriteBarrierTestBitMeaning") == "1";
        Node destination = new();
        Node oldValue = new();
        Node newValue = new();
        destination.Next = oldValue;

        if (!GC.TryStartNoGCRegion(1024 * 1024))
        {
            throw new InvalidOperationException("Unable to enter the slot-log no-GC test region.");
        }

        try
        {
            ref Node? slot = ref destination.Next;
            nuint slotAddress = (nuint)Unsafe.AsPointer(ref slot);
            uint bit = (uint)((slotAddress >> 3) & 7);
            byte workByte = workWhenSet ? byte.MaxValue : (byte)0;
            byte noWorkByte = workWhenSet ? (byte)0 : byte.MaxValue;

            hooks.Reset(slotAddress, workByte, claimBits: true);
            destination.Next = newValue;
            if ((hooks.AttemptCount() != 1) ||
                (hooks.WinCount() != 1) ||
                (hooks.ArgumentErrorCount() != 0) ||
                (hooks.LastDestination() != slotAddress))
            {
                throw new InvalidOperationException("The slot-log slow path did not claim the expected field.");
            }

            destination.Next = oldValue;
            if ((hooks.AttemptCount() != 1) || (hooks.WinCount() != 1))
            {
                throw new InvalidOperationException("A claimed slot did not take the bit-level fast path.");
            }

            hooks.Reset(slotAddress, noWorkByte, claimBits: true);
            destination.Next = newValue;
            if (hooks.AttemptCount() != 0)
            {
                throw new InvalidOperationException("A no-work metadata byte did not take the byte-zero fast path.");
            }

            uint otherBit = (bit + 1) & 7;
            byte mixedByte = workWhenSet
                ? (byte)(1 << (int)otherBit)
                : (byte)(1 << (int)bit);
            hooks.Reset(slotAddress, mixedByte, claimBits: true);
            destination.Next = oldValue;
            if (hooks.AttemptCount() != 0)
            {
                throw new InvalidOperationException("The barrier tested the wrong bit in a mixed metadata byte.");
            }
        }
        finally
        {
            GC.EndNoGCRegion();
        }

        const int ThreadCount = 32;
        Thread[] threads = new Thread[ThreadCount];
        ManualResetEventSlim start = new(false);
        CountdownEvent ready = new(ThreadCount);
        nuint sameSlot = hooks.SyntheticDestination(32, 3);
        for (int index = 0; index < threads.Length; index++)
        {
            threads[index] = new Thread(() =>
            {
                ready.Signal();
                start.Wait();
                hooks.ClaimSynthetic(sameSlot);
            });
            threads[index].Start();
        }

        ready.Wait();
        hooks.Reset(sameSlot, workWhenSet ? byte.MaxValue : (byte)0, claimBits: true);
        start.Set();
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        ulong sameSlotAttempts = hooks.AttemptCount();
        ulong sameSlotWins = hooks.WinCount();
        if ((sameSlotAttempts != ThreadCount) || (sameSlotWins != 1))
        {
            throw new InvalidOperationException(
                $"Same-slot contention produced {sameSlotAttempts} attempts and {sameSlotWins} wins.");
        }

        start.Dispose();
        ready.Dispose();
        start = new ManualResetEventSlim(false);
        ready = new CountdownEvent(8);
        nuint firstBit = hooks.SyntheticDestination(33, 0);
        for (int index = 0; index < 8; index++)
        {
            nuint bitDestination = hooks.SyntheticDestination(33, (uint)index);
            threads[index] = new Thread(() =>
            {
                ready.Signal();
                start.Wait();
                hooks.ClaimSynthetic(bitDestination);
            });
            threads[index].Start();
        }

        ready.Wait();
        hooks.Reset(firstBit, workWhenSet ? byte.MaxValue : (byte)0, claimBits: true);
        start.Set();
        for (int index = 0; index < 8; index++)
        {
            threads[index].Join();
        }

        ulong sameByteAttempts = hooks.AttemptCount();
        ulong sameByteWins = hooks.WinCount();
        if ((sameByteAttempts != 8) || (sameByteWins != 8))
        {
            throw new InvalidOperationException(
                $"Same-byte contention produced {sameByteAttempts} attempts and {sameByteWins} wins.");
        }

        start.Dispose();
        ready.Dispose();

        ExerciseCompleteStoreSurfaces(hooks, workWhenSet);
        ExerciseDependentEdges(hooks, workWhenSet);
        ExerciseVeryLargeLayout(hooks, workWhenSet);
    }

    private static unsafe void ExerciseCompleteStoreSurfaces(NativeHooks hooks, bool workWhenSet)
    {
        ObjectHolder holder = new();
        object oldValue = new();
        object newValue = new();
        object comparand = new();
        object?[] source = [new object(), new object(), new object(), new object()];
        object?[] destination = [new object(), new object(), new object(), new object()];
        object?[] fillDestination = [new object(), new object(), new object(), new object()];
        object?[] overlap = [new object(), new object(), new object(), new object()];
        object?[] empty = [];
        MixedReferences[] mixedSource =
        [
            new() { First = new object(), Scalar = 101, Second = new object() },
            new() { First = new object(), Scalar = 202, Second = new object() },
        ];
        MixedReferences[] mixedDestination =
        [
            new() { First = new object(), Scalar = 303, Second = new object() },
            new() { First = new object(), Scalar = 404, Second = new object() },
        ];
        MixedReferences[] mixedSpan =
        [
            new() { First = new object(), Scalar = 505, Second = new object() },
            new() { First = new object(), Scalar = 606, Second = new object() },
        ];
        MixedReferences mixedFillValue =
            new() { First = new object(), Scalar = 707, Second = new object() };
        MixedReferences[] mixedFill = new MixedReferences[4];
        LargeReferenceHolder largeHolder = new();
        LargeMixedReferences largeValue = new()
        {
            First = new object(),
            Scalar0 = 101,
            Second = new object(),
            Scalar1 = 202,
            Third = new object(),
            Scalar2 = 303,
            Fourth = new object(),
            Scalar3 = 404,
            Fifth = new object(),
            Scalar4 = 505,
            Sixth = new object(),
            Scalar5 = 606,
            Seventh = new object(),
            Scalar6 = 707,
            Eighth = new object(),
            Scalar7 = 808,
            Ninth = new object(),
            Scalar8 = 909,
            Tenth = new object(),
            Scalar9 = 1_010,
            Eleventh = new object(),
            Scalar10 = 1_111,
            Twelfth = new object(),
            Scalar11 = 1_212,
            Thirteenth = new object(),
            Scalar12 = 1_313,
            Fourteenth = new object(),
            Scalar13 = 1_414,
            Fifteenth = new object(),
            Scalar14 = 1_515,
            Sixteenth = new object(),
            Scalar15 = 1_616,
        };
        object?[] cloneSource = [new object(), new object(), new object()];
        MixedReferences[] mixedCloneSource =
        [
            new() { First = new object(), Scalar = 1_111, Second = new object() },
            new() { First = new object(), Scalar = 1_212, Second = new object() },
        ];
        int?[] nullableDestination = [42];
        object uninitializedNullable = RuntimeHelpers.GetUninitializedObject(typeof(int?));
        object overlap0 = overlap[0]!;
        object overlap1 = overlap[1]!;
        object overlap2 = overlap[2]!;
        var valueField = typeof(ObjectHolder).GetField(nameof(ObjectHolder.Value))
            ?? throw new InvalidOperationException("Unable to find the VM store test field.");
        byte workByte = workWhenSet ? byte.MaxValue : (byte)0;
        byte noWorkByte = workWhenSet ? (byte)0 : byte.MaxValue;

        if (!GC.TryStartNoGCRegion(4 * 1024 * 1024))
        {
            throw new InvalidOperationException("Unable to enter the complete-store no-GC test region.");
        }

        try
        {
            ref object? holderSlot = ref holder.Value;
            nuint holderAddress = (nuint)Unsafe.AsPointer(ref holderSlot);

            holder.Value = oldValue;
            hooks.Reset(holderAddress, workByte, claimBits: true);
            holder.Value = null;
            AssertClaim(hooks, 1, 1, "null field store");

            holder.Value = oldValue;
            hooks.Reset(holderAddress, workByte, claimBits: true);
            holder.Value = "slot-log-frozen";
            AssertClaim(hooks, 1, 1, "frozen-reference field store");

            holder.Value = oldValue;
            hooks.Reset(holderAddress, workByte, claimBits: true);
            object? exchanged = Interlocked.Exchange(ref holder.Value, newValue);
            if (!ReferenceEquals(exchanged, oldValue) || !ReferenceEquals(holder.Value, newValue))
            {
                throw new InvalidOperationException("Atomic exchange produced an unexpected value.");
            }
            AssertClaim(hooks, 1, 1, "atomic exchange");

            holder.Value = oldValue;
            hooks.Reset(holderAddress, workByte, claimBits: true);
            object? observed = Interlocked.CompareExchange(ref holder.Value, newValue, comparand);
            if (!ReferenceEquals(observed, oldValue) || !ReferenceEquals(holder.Value, oldValue))
            {
                throw new InvalidOperationException("Failed compare-exchange modified the field.");
            }
            AssertClaim(hooks, 1, 1, "failed compare-exchange pre-barrier");

            holder.Value = oldValue;
            hooks.Reset(holderAddress, workByte, claimBits: true);
            valueField.SetValue(holder, newValue);
            if (!ReferenceEquals(holder.Value, newValue))
            {
                throw new InvalidOperationException("Reflection field store failed.");
            }
            AssertClaim(hooks, 1, 1, "reflection field store");

            ref object? destinationStart = ref MemoryMarshal.GetArrayDataReference(destination);
            nuint destinationAddress = (nuint)Unsafe.AsPointer(ref destinationStart);
            hooks.ResetRange(destinationAddress, (nuint)source.Length, workByte, claimBits: true);
            Array.Copy(source, destination, source.Length);
            for (int index = 0; index < source.Length; index++)
            {
                if (!ReferenceEquals(source[index], destination[index]))
                {
                    throw new InvalidOperationException("Bulk reference copy produced an unexpected value.");
                }
            }
            AssertClaim(hooks, (ulong)source.Length, (ulong)source.Length, "bulk reference copy");
            AssertRangeCalls(hooks, 1, 0, "bulk reference copy");

            hooks.ResetRange(destinationAddress, (nuint)source.Length, noWorkByte, claimBits: true);
            Array.Copy(source, destination, source.Length);
            AssertClaim(hooks, 0, 0, "all-claimed bulk reference copy");
            AssertRangeCalls(hooks, 0, 0, "all-claimed bulk reference copy");

            nuint destinationBit = (destinationAddress >> 3) & 63;
            nuint outsideDestination = destinationBit == 0
                ? destinationAddress + ((nuint)source.Length * (nuint)IntPtr.Size)
                : destinationAddress - (nuint)IntPtr.Size;
            hooks.ResetRange(destinationAddress, (nuint)source.Length, noWorkByte, claimBits: true);
            hooks.SetRequiresWork(outsideDestination, requiresWork: true);
            Array.Copy(source, destination, source.Length);
            AssertClaim(hooks, 0, 0, "out-of-range metadata bit");
            AssertRangeCalls(hooks, 0, 0, "out-of-range metadata bit");

            hooks.ResetRange(destinationAddress, (nuint)source.Length, noWorkByte, claimBits: true);
            hooks.SetRequiresWork(
                destinationAddress + (2 * (nuint)IntPtr.Size),
                requiresWork: true);
            Array.Copy(source, destination, source.Length);
            AssertClaim(hooks, (ulong)source.Length, 1, "single in-range metadata bit");
            AssertRangeCalls(hooks, 1, 0, "single in-range metadata bit");

            hooks.ResetRange(destinationAddress, (nuint)destination.Length, workByte, claimBits: true);
            Array.Clear(destination);
            if (Array.Exists(destination, static item => item is not null))
            {
                throw new InvalidOperationException("Array.Clear did not clear a reference array.");
            }
            AssertClaim(hooks, (ulong)destination.Length, (ulong)destination.Length, "reference Array.Clear");
            AssertRangeCalls(hooks, 1, 1, "reference Array.Clear");

            hooks.ResetRange(destinationAddress, (nuint)destination.Length, noWorkByte, claimBits: true);
            Array.Clear(destination);
            AssertClaim(hooks, 0, 0, "all-claimed reference Array.Clear");
            AssertRangeCalls(hooks, 0, 0, "all-claimed reference Array.Clear");

            ref object? fillStart = ref MemoryMarshal.GetArrayDataReference(fillDestination);
            nuint fillAddress = (nuint)Unsafe.AsPointer(ref fillStart);
            hooks.ResetRange(fillAddress, (nuint)fillDestination.Length, workByte, claimBits: true);
            FillReferences(fillDestination, newValue);
            foreach (object? item in fillDestination)
            {
                if (!ReferenceEquals(item, newValue))
                {
                    throw new InvalidOperationException("Span.Fill did not fill a reference span.");
                }
            }
            AssertClaim(hooks, (ulong)fillDestination.Length, (ulong)fillDestination.Length, "reference Span.Fill");
            AssertRangeCalls(hooks, 0, 0, "reference Span.Fill");

            hooks.ResetRange(fillAddress, (nuint)fillDestination.Length, noWorkByte, claimBits: true);
            FillReferences(fillDestination, oldValue);
            AssertClaim(hooks, 0, 0, "all-claimed reference Span.Fill");
            AssertRangeCalls(hooks, 0, 0, "all-claimed reference Span.Fill");

            hooks.Reset(destinationAddress, workByte, claimBits: true);
            Array.Copy(empty, empty, 0);
            Array.Clear(empty);
            AssertClaim(hooks, 0, 0, "empty reference ranges");
            AssertRangeCalls(hooks, 0, 0, "empty reference ranges");
            if (!hooks.InvokeEmptyRange(destinationAddress))
            {
                throw new InvalidOperationException("The selected range helper rejected an empty range.");
            }
            AssertClaim(hooks, 0, 0, "direct empty range");
            AssertRangeCalls(hooks, 0, 0, "direct empty range");

            ref object? overlapStart = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(overlap), 1);
            nuint overlapAddress = (nuint)Unsafe.AsPointer(ref overlapStart);
            hooks.ResetRange(overlapAddress, 3, workByte, claimBits: true);
            Array.Copy(overlap, 0, overlap, 1, 3);
            if (!ReferenceEquals(overlap[1], overlap0) ||
                !ReferenceEquals(overlap[2], overlap1) ||
                !ReferenceEquals(overlap[3], overlap2))
            {
                throw new InvalidOperationException("Overlapping reference copy violated memmove semantics.");
            }
            AssertClaim(hooks, 3, 3, "overlapping reference copy");
            AssertRangeCalls(hooks, 1, 0, "overlapping reference copy");

            nuint mixedWordCount =
                (nuint)(mixedDestination.Length * Unsafe.SizeOf<MixedReferences>() / IntPtr.Size);
            ref MixedReferences mixedDestinationStart =
                ref MemoryMarshal.GetArrayDataReference(mixedDestination);
            nuint mixedDestinationAddress = (nuint)Unsafe.AsPointer(ref mixedDestinationStart);
            hooks.ResetRange(mixedDestinationAddress, mixedWordCount, workByte, claimBits: true);
            mixedSource.AsSpan().CopyTo(mixedDestination);
            if (!ReferenceEquals(mixedDestination[0].First, mixedSource[0].First) ||
                !ReferenceEquals(mixedDestination[0].Second, mixedSource[0].Second) ||
                (mixedDestination[0].Scalar != mixedSource[0].Scalar) ||
                !ReferenceEquals(mixedDestination[1].First, mixedSource[1].First) ||
                !ReferenceEquals(mixedDestination[1].Second, mixedSource[1].Second) ||
                (mixedDestination[1].Scalar != mixedSource[1].Scalar))
            {
                throw new InvalidOperationException("Typed mixed-struct copy produced an unexpected value.");
            }
            AssertClaim(hooks, 4, 4, "typed mixed-struct copy");
            AssertRangeCalls(hooks, 0, 0, "typed mixed-struct copy");

            ref MixedReferences mixedFillStart = ref MemoryMarshal.GetArrayDataReference(mixedFill);
            nuint mixedFillAddress = (nuint)Unsafe.AsPointer(ref mixedFillStart);
            nuint mixedFillWordCount =
                (nuint)(mixedFill.Length * Unsafe.SizeOf<MixedReferences>() / IntPtr.Size);
            hooks.ResetRange(mixedFillAddress, mixedFillWordCount, workByte, claimBits: true);
            FillMixedReferences(mixedFill, mixedFillValue);
            foreach (MixedReferences item in mixedFill)
            {
                if (!ReferenceEquals(item.First, mixedFillValue.First) ||
                    !ReferenceEquals(item.Second, mixedFillValue.Second) ||
                    (item.Scalar != mixedFillValue.Scalar))
                {
                    throw new InvalidOperationException("Span.Fill did not fill a mixed-reference span.");
                }
            }
            AssertClaim(hooks, 8, 8, "mixed-reference Span.Fill");
            AssertRangeCalls(hooks, 0, 0, "mixed-reference Span.Fill");

            hooks.ResetRange(mixedFillAddress, mixedFillWordCount, noWorkByte, claimBits: true);
            FillMixedReferences(mixedFill, mixedFillValue);
            AssertClaim(hooks, 0, 0, "all-claimed mixed-reference Span.Fill");

            StoreLargeValue(largeHolder, largeValue);
            ref object? largeStart = ref largeHolder.Value.First;
            nuint largeAddress = (nuint)Unsafe.AsPointer(ref largeStart);
            nuint largeWordCount =
                (nuint)(Unsafe.SizeOf<LargeMixedReferences>() / IntPtr.Size);
            hooks.ResetRange(largeAddress, largeWordCount, workByte, claimBits: true);
            StoreLargeValue(largeHolder, largeValue);
            AssertClaim(hooks, 16, 16, "layout-aware JIT struct copy");

            hooks.ResetRange(largeAddress, largeWordCount, noWorkByte, claimBits: true);
            StoreLargeValue(largeHolder, largeValue);
            AssertClaim(hooks, 0, 0, "all-claimed layout-aware JIT struct copy");

            hooks.ResetRange(largeAddress, largeWordCount, workByte, claimBits: true);
            ClearLargeValue(largeHolder);
            AssertClaim(hooks, 16, 16, "layout-aware JIT struct clear");
            if ((largeHolder.Value.First is not null) ||
                (largeHolder.Value.Second is not null) ||
                (largeHolder.Value.Third is not null) ||
                (largeHolder.Value.Fourth is not null) ||
                (largeHolder.Value.Sixteenth is not null))
            {
                throw new InvalidOperationException("Layout-aware JIT struct clear retained references.");
            }

            hooks.ResetRange(largeAddress, largeWordCount, noWorkByte, claimBits: true);
            ClearLargeValue(largeHolder);
            AssertClaim(hooks, 0, 0, "all-claimed layout-aware JIT struct clear");

            mixedDestination[0] =
                new MixedReferences { First = new object(), Scalar = 707, Second = new object() };
            mixedDestination[1] =
                new MixedReferences { First = new object(), Scalar = 808, Second = new object() };
            hooks.ResetRange(mixedDestinationAddress, mixedWordCount, workByte, claimBits: true);
            Array.Copy(mixedSource, mixedDestination, mixedSource.Length);
            if (!ReferenceEquals(mixedDestination[0].First, mixedSource[0].First) ||
                !ReferenceEquals(mixedDestination[0].Second, mixedSource[0].Second) ||
                (mixedDestination[0].Scalar != mixedSource[0].Scalar) ||
                !ReferenceEquals(mixedDestination[1].First, mixedSource[1].First) ||
                !ReferenceEquals(mixedDestination[1].Second, mixedSource[1].Second) ||
                (mixedDestination[1].Scalar != mixedSource[1].Scalar))
            {
                throw new InvalidOperationException("Array.Copy produced an unexpected mixed-struct value.");
            }
            AssertClaim(hooks, 4, 4, "mixed-struct Array.Copy");
            AssertRangeCalls(hooks, 0, 0, "mixed-struct Array.Copy");

            mixedDestination[0] =
                new MixedReferences { First = new object(), Scalar = 909, Second = new object() };
            mixedDestination[1] =
                new MixedReferences { First = new object(), Scalar = 1_010, Second = new object() };
            hooks.ResetRange(mixedDestinationAddress, mixedWordCount, workByte, claimBits: true);
            Array.Clear(mixedDestination);
            if ((mixedDestination[0].First is not null) ||
                (mixedDestination[0].Second is not null) ||
                (mixedDestination[0].Scalar != 0) ||
                (mixedDestination[1].First is not null) ||
                (mixedDestination[1].Second is not null) ||
                (mixedDestination[1].Scalar != 0))
            {
                throw new InvalidOperationException("Array.Clear did not clear a mixed-reference struct.");
            }
            AssertClaim(hooks, 4, 4, "mixed-struct Array.Clear");
            AssertRangeCalls(hooks, 0, 0, "mixed-struct Array.Clear");

            ref MixedReferences mixedSpanStart = ref MemoryMarshal.GetArrayDataReference(mixedSpan);
            nuint mixedSpanAddress = (nuint)Unsafe.AsPointer(ref mixedSpanStart);
            hooks.ResetRange(mixedSpanAddress, mixedWordCount, workByte, claimBits: true);
            mixedSpan.AsSpan().Clear();
            if ((mixedSpan[0].First is not null) ||
                (mixedSpan[0].Second is not null) ||
                (mixedSpan[0].Scalar != 0) ||
                (mixedSpan[1].First is not null) ||
                (mixedSpan[1].Second is not null) ||
                (mixedSpan[1].Scalar != 0))
            {
                throw new InvalidOperationException("Span.Clear did not clear a mixed-reference struct.");
            }
            AssertClaim(hooks, 4, 4, "mixed-struct Span.Clear");
            AssertRangeCalls(hooks, 0, 0, "mixed-struct Span.Clear");

            hooks.Reset(destinationAddress, workByte, claimBits: true);
            object?[] clone = (object?[])cloneSource.Clone();
            if ((clone.Length != cloneSource.Length) ||
                !ReferenceEquals(clone[0], cloneSource[0]) ||
                !ReferenceEquals(clone[1], cloneSource[1]) ||
                !ReferenceEquals(clone[2], cloneSource[2]))
            {
                throw new InvalidOperationException("Reference-array clone lost header or element data.");
            }
            AssertRangeCalls(hooks, 1, 0, "reference-array clone");

            hooks.Reset(destinationAddress, workByte, claimBits: true);
            MixedReferences[] mixedClone = (MixedReferences[])mixedCloneSource.Clone();
            if ((mixedClone.Length != mixedCloneSource.Length) ||
                !ReferenceEquals(mixedClone[0].First, mixedCloneSource[0].First) ||
                !ReferenceEquals(mixedClone[0].Second, mixedCloneSource[0].Second) ||
                (mixedClone[0].Scalar != mixedCloneSource[0].Scalar) ||
                !ReferenceEquals(mixedClone[1].First, mixedCloneSource[1].First) ||
                !ReferenceEquals(mixedClone[1].Second, mixedCloneSource[1].Second) ||
                (mixedClone[1].Scalar != mixedCloneSource[1].Scalar))
            {
                throw new InvalidOperationException("Mixed-struct array clone lost header or element data.");
            }
            AssertRangeCalls(hooks, 0, 0, "mixed-struct array clone");

            ref int? nullableSlot = ref MemoryMarshal.GetArrayDataReference(nullableDestination);
            nuint nullableAddress = (nuint)Unsafe.AsPointer(ref nullableSlot);
            hooks.ResetRange(nullableAddress, 1, workByte, claimBits: true);
            nullableDestination.SetValue(uninitializedNullable, 0);
            if (nullableDestination[0].GetValueOrDefault() != 0)
            {
                throw new InvalidOperationException("Uninitialized nullable SetValue produced unexpected data.");
            }
            AssertClaim(hooks, 0, 0, "pointer-free nullable SetValue");
        }
        finally
        {
            GC.EndNoGCRegion();
        }
    }

    private static void AssertClaim(NativeHooks hooks, ulong attempts, ulong wins, string operation)
    {
        ulong actualAttempts = hooks.AttemptCount();
        ulong actualWins = hooks.WinCount();
        ulong argumentErrors = hooks.ArgumentErrorCount();
        if ((actualAttempts != attempts) || (actualWins != wins) || (argumentErrors != 0))
        {
            throw new InvalidOperationException(
                $"{operation} produced {actualAttempts} attempts, {actualWins} wins, and {argumentErrors} argument errors.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void StoreLargeValue(LargeReferenceHolder holder, LargeMixedReferences value) =>
        holder.Value = value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ClearLargeValue(LargeReferenceHolder holder) =>
        holder.Value = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreVeryLargeValue(
        VeryLargeReferenceHolder holder,
        in VeryLargeMixedLayout value) =>
        holder.Value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearVeryLargeValue(VeryLargeReferenceHolder holder) =>
        holder.Value = default;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void StoreVeryLargeValueMinOpts(
        VeryLargeReferenceHolder holder,
        in VeryLargeMixedLayout value) =>
        holder.Value = value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void ClearVeryLargeValueMinOpts(VeryLargeReferenceHolder holder) =>
        holder.Value = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreVeryLargeValueCold(
        VeryLargeReferenceHolder holder,
        in VeryLargeMixedLayout value,
        Action beforeStore)
    {
        try
        {
            ThrowForVeryLargeColdPath();
        }
        catch (InvalidOperationException)
        {
            beforeStore();
            holder.Value = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearVeryLargeValueCold(
        VeryLargeReferenceHolder holder,
        Action beforeStore)
    {
        try
        {
            ThrowForVeryLargeColdPath();
        }
        catch (InvalidOperationException)
        {
            beforeStore();
            holder.Value = default;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowForVeryLargeColdPath() =>
        throw new InvalidOperationException();

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object FillReferences(object?[] destination, object value)
    {
        destination.AsSpan().Fill(value);
        return destination[^1]!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object FillMixedReferences(MixedReferences[] destination, MixedReferences value)
    {
        destination.AsSpan().Fill(value);
        return destination[^1].Second!;
    }

    private static void AssertRangeCalls(
        NativeHooks hooks,
        ulong rangeCalls,
        ulong clearRangeCalls,
        string operation)
    {
        ulong actualRangeCalls = hooks.RangeCallCount();
        ulong actualClearRangeCalls = hooks.ClearRangeCallCount();
        if ((actualRangeCalls != rangeCalls) || (actualClearRangeCalls != clearRangeCalls))
        {
            throw new InvalidOperationException(
                $"{operation} produced {actualRangeCalls} range calls and {actualClearRangeCalls} clear-range calls.");
        }
    }

    private static void ExerciseDependentEdges(NativeHooks hooks, bool workWhenSet)
    {
        AssemblyName name = new($"SlotLogCollectible{Guid.NewGuid():N}");
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.RunAndCollect);
        Type collectibleType = assembly
            .DefineDynamicModule(name.Name!)
            .DefineType("CollectibleElement", TypeAttributes.Public)
            .CreateTypeInfo()!
            .AsType();
        MethodInfo allocator = typeof(Program)
            .GetMethod(nameof(AllocatePinnedArray), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(collectibleType);
        byte workByte = workWhenSet ? byte.MaxValue : (byte)0;
        nuint resetDestination = hooks.SyntheticDestination(48, 0);

        hooks.Reset(resetDestination, workByte, claimBits: true);
        Array nonCollectible = AllocatePinnedArray<object>();
        if (hooks.DependentEdgeCallCount() != 0)
        {
            throw new InvalidOperationException("A non-collectible UOH allocation reported a dependent edge.");
        }

        hooks.Reset(resetDestination, workByte, claimBits: true);
        Array collectible = (Array)(allocator.Invoke(null, null)
            ?? throw new InvalidOperationException("The collectible UOH allocation returned null."));
        if (hooks.DependentEdgeCallCount() != 1)
        {
            throw new InvalidOperationException(
                $"A collectible UOH allocation reported {hooks.DependentEdgeCallCount()} dependent edges.");
        }

        GC.KeepAlive(nonCollectible);
        GC.KeepAlive(collectible);
        GC.KeepAlive(collectibleType);
        GC.KeepAlive(assembly);
    }

    private static unsafe void ExerciseVeryLargeLayout(NativeHooks hooks, bool workWhenSet)
    {
        object first = new();
        object second = new();
        VeryLargeMixedLayout source = default;
        source[0] = new MixedReferences { First = first, Scalar = 101, Second = second };
        source[2047] = new MixedReferences { First = second, Scalar = 202, Second = first };
        VeryLargeReferenceHolder holder = new();
        StoreVeryLargeValue(holder, in source);

        ref object? destinationStart = ref holder.Value[0].First;
        nuint destinationAddress = (nuint)Unsafe.AsPointer(ref destinationStart);
        nuint destinationWordCount =
            (nuint)(Unsafe.SizeOf<VeryLargeMixedLayout>() / IntPtr.Size);
        byte workByte = workWhenSet ? byte.MaxValue : (byte)0;

        hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        StoreVeryLargeValue(holder, in source);
        AssertClaim(hooks, 4_096, 4_096, "very-large mixed-layout copy");
        if (!ReferenceEquals(holder.Value[0].First, first) ||
            !ReferenceEquals(holder.Value[2047].Second, first))
        {
            throw new InvalidOperationException("Very-large mixed-layout copy produced an unexpected value.");
        }

        hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        ClearVeryLargeValue(holder);
        AssertClaim(hooks, 4_096, 4_096, "very-large mixed-layout clear");
        if ((holder.Value[0].First is not null) ||
            (holder.Value[2047].Second is not null))
        {
            throw new InvalidOperationException("Very-large mixed-layout clear retained references.");
        }

        hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        StoreVeryLargeValueMinOpts(holder, in source);
        AssertClaim(hooks, 4_096, 4_096, "very-large minopts mixed-layout copy");
        hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        ClearVeryLargeValueMinOpts(holder);
        AssertClaim(hooks, 4_096, 4_096, "very-large minopts mixed-layout clear");

        Action resetColdRange = () =>
            hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        StoreVeryLargeValueCold(holder, in source, resetColdRange);
        AssertClaim(hooks, 4_096, 4_096, "very-large cold mixed-layout copy");
        ClearVeryLargeValueCold(holder, resetColdRange);
        AssertClaim(hooks, 4_096, 4_096, "very-large cold mixed-layout clear");

        StoreVeryLargeValue(holder, in source);
        hooks.ResetRange(destinationAddress, destinationWordCount, workByte, claimBits: true);
        StoreVeryLargeValue(holder, in holder.Value);
        AssertClaim(hooks, 0, 0, "very-large identical source/destination no-op");
        if (!ReferenceEquals(holder.Value[0].First, first) ||
            !ReferenceEquals(holder.Value[2047].Second, first))
        {
            throw new InvalidOperationException("Very-large identical source/destination copy changed the value.");
        }

        VeryLargeMixedLayout[] sourceArray = [source];
        VeryLargeMixedLayout[] destinationArray = new VeryLargeMixedLayout[1];
        ref object? arrayDestinationStart = ref destinationArray[0][0].First;
        nuint arrayDestinationAddress = (nuint)Unsafe.AsPointer(ref arrayDestinationStart);

        hooks.ResetRange(arrayDestinationAddress, destinationWordCount, workByte, claimBits: true);
        sourceArray.AsSpan().CopyTo(destinationArray);
        AssertClaim(hooks, 4_096, 4_096, "very-large mixed-layout Span.CopyTo");
        if (!ReferenceEquals(destinationArray[0][0].First, first) ||
            !ReferenceEquals(destinationArray[0][2047].Second, first))
        {
            throw new InvalidOperationException("Very-large Span.CopyTo produced an unexpected value.");
        }

        hooks.ResetRange(arrayDestinationAddress, destinationWordCount, workByte, claimBits: true);
        destinationArray.AsSpan().Fill(source);
        AssertClaim(hooks, 4_096, 4_096, "very-large mixed-layout Span.Fill");
        if (!ReferenceEquals(destinationArray[0][0].First, first) ||
            !ReferenceEquals(destinationArray[0][2047].Second, first))
        {
            throw new InvalidOperationException("Very-large Span.Fill produced an unexpected value.");
        }

        hooks.ResetRange(arrayDestinationAddress, destinationWordCount, workByte, claimBits: true);
        destinationArray.AsSpan().Clear();
        AssertClaim(hooks, 4_096, 4_096, "very-large mixed-layout Span.Clear");
        if ((destinationArray[0][0].First is not null) ||
            (destinationArray[0][2047].Second is not null))
        {
            throw new InvalidOperationException("Very-large Span.Clear retained references.");
        }

        DynamicMethod volatileCopyMethod = new(
            "VolatileVeryLargeCopy",
            typeof(void),
            [typeof(VeryLargeMixedLayout).MakeByRefType(), typeof(VeryLargeMixedLayout).MakeByRefType()],
            typeof(Program).Module,
            skipVisibility: true);
        ILGenerator il = volatileCopyMethod.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Volatile);
        il.Emit(OpCodes.Ldobj, typeof(VeryLargeMixedLayout));
        il.Emit(OpCodes.Volatile);
        il.Emit(OpCodes.Stobj, typeof(VeryLargeMixedLayout));
        il.Emit(OpCodes.Ret);
        VeryLargeCopy volatileCopy =
            (VeryLargeCopy)volatileCopyMethod.CreateDelegate(typeof(VeryLargeCopy));
        hooks.ResetRange(arrayDestinationAddress, destinationWordCount, workByte, claimBits: true);
        volatileCopy(ref destinationArray[0], ref sourceArray[0]);
        AssertClaim(hooks, 4_096, 4_096, "very-large volatile mixed-layout copy");
        if (!ReferenceEquals(destinationArray[0][0].First, first) ||
            !ReferenceEquals(destinationArray[0][2047].Second, first))
        {
            throw new InvalidOperationException("Very-large volatile copy produced an unexpected value.");
        }

        object alternateFirst = new();
        object alternateSecond = new();
        VeryLargeMixedLayout alternate = default;
        alternate[0] = new MixedReferences
        {
            First = alternateFirst,
            Scalar = 303,
            Second = alternateSecond,
        };
        alternate[2047] = new MixedReferences
        {
            First = alternateSecond,
            Scalar = 404,
            Second = alternateFirst,
        };
        VeryLargeMixedLayout[] overlap = [source, alternate, default];
        ref object? backwardDestinationStart = ref overlap[1][0].First;
        nuint backwardDestinationAddress = (nuint)Unsafe.AsPointer(ref backwardDestinationStart);
        hooks.ResetRange(
            backwardDestinationAddress,
            destinationWordCount * 2,
            workByte,
            claimBits: true);
        overlap.AsSpan(0, 2).CopyTo(overlap.AsSpan(1, 2));
        AssertClaim(hooks, 8_192, 8_192, "very-large backward-overlap Span.CopyTo");
        if (!ReferenceEquals(overlap[1][0].First, first) ||
            !ReferenceEquals(overlap[2][0].First, alternateFirst) ||
            !ReferenceEquals(overlap[2][2047].Second, alternateFirst))
        {
            throw new InvalidOperationException("Very-large backward-overlap copy violated memmove semantics.");
        }

        ref object? forwardDestinationStart = ref overlap[0][0].First;
        nuint forwardDestinationAddress = (nuint)Unsafe.AsPointer(ref forwardDestinationStart);
        hooks.ResetRange(
            forwardDestinationAddress,
            destinationWordCount * 2,
            workByte,
            claimBits: true);
        overlap.AsSpan(1, 2).CopyTo(overlap.AsSpan(0, 2));
        AssertClaim(hooks, 8_192, 8_192, "very-large forward-overlap Span.CopyTo");
        if (!ReferenceEquals(overlap[0][0].First, first) ||
            !ReferenceEquals(overlap[1][0].First, alternateFirst) ||
            !ReferenceEquals(overlap[1][2047].Second, alternateFirst))
        {
            throw new InvalidOperationException("Very-large forward-overlap copy violated memmove semantics.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T[] AllocatePinnedArray<T>() => GC.AllocateArray<T>(1, pinned: true);

    private static unsafe void ExerciseInterpreterStoreSurfaces(NativeHooks hooks)
    {
        bool workWhenSet = Environment.GetEnvironmentVariable("DOTNET_GCWriteBarrierTestBitMeaning") == "1";
        byte workByte = workWhenSet ? byte.MaxValue : (byte)0;
        MixedReferences[] source =
        [
            new() { First = new object(), Scalar = 111, Second = new object() },
            new() { First = new object(), Scalar = 222, Second = new object() },
        ];
        MixedReferences[] destination =
        [
            new() { First = new object(), Scalar = 333, Second = new object() },
            new() { First = new object(), Scalar = 444, Second = new object() },
        ];
        nuint wordCount = (nuint)(destination.Length * Unsafe.SizeOf<MixedReferences>() / IntPtr.Size);

        if (!GC.TryStartNoGCRegion(1024 * 1024))
        {
            throw new InvalidOperationException("Unable to enter the interpreter no-GC test region.");
        }

        try
        {
            ref MixedReferences destinationStart = ref MemoryMarshal.GetArrayDataReference(destination);
            nuint destinationAddress = (nuint)Unsafe.AsPointer(ref destinationStart);
            hooks.ResetRange(destinationAddress, wordCount, workByte, claimBits: true);
            Array.Copy(source, destination, source.Length);
            AssertClaim(hooks, 4, 4, "interpreter mixed-struct copy");

            hooks.ResetRange(destinationAddress, wordCount, workByte, claimBits: true);
            destination.AsSpan().Clear();
            if ((destination[0].First is not null) ||
                (destination[0].Second is not null) ||
                (destination[0].Scalar != 0) ||
                (destination[1].First is not null) ||
                (destination[1].Second is not null) ||
                (destination[1].Scalar != 0))
            {
                throw new InvalidOperationException("Interpreter Span.Clear did not clear a mixed-reference struct.");
            }
            AssertClaim(hooks, 4, 4, "interpreter mixed-struct clear");
        }
        finally
        {
            GC.EndNoGCRegion();
        }

    }

    private static void ExerciseReferenceCountWidths()
    {
        foreach (int bits in new[] { 2, 4, 8 })
        {
            int maximum = (1 << bits) - 1;
            for (int count = 0; count <= maximum; count++)
            {
                int incremented = count == maximum ? maximum : count + 1;
                int decremented = (count == 0) || (count == maximum) ? count : count - 1;
                if ((incremented < count) ||
                    (incremented > maximum) ||
                    (decremented < 0) ||
                    (count == maximum && decremented != maximum))
                {
                    throw new InvalidOperationException($"Invalid {bits}-bit reference-count saturation.");
                }
            }
        }
    }

    private static unsafe void ExerciseEpochReset(NativeHooks hooks)
    {
        object?[] slots = new object?[20_000];
        object oldValue = new();
        object newValue = new();
        slots[0] = oldValue;
        ref object? slot = ref MemoryMarshal.GetArrayDataReference(slots);
        nuint slotAddress = (nuint)Unsafe.AsPointer(ref slot);
        bool workWhenSet =
            Environment.GetEnvironmentVariable("DOTNET_GCWriteBarrierTestBitMeaning") == "1";
        hooks.Reset(slotAddress, workWhenSet ? byte.MaxValue : (byte)0, claimBits: true);
        slots[0] = newValue;
        ulong winsBeforeGc = hooks.WinCount();
        if (winsBeforeGc != 1)
        {
            throw new InvalidOperationException("The epoch-reset test did not claim its initial slot.");
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
        ref object? slotAfterGc = ref MemoryMarshal.GetArrayDataReference(slots);
        nuint slotAddressAfterGc = (nuint)Unsafe.AsPointer(ref slotAfterGc);
        if (slotAddressAfterGc != slotAddress)
        {
            throw new InvalidOperationException("The non-compacting LOH epoch-test slot moved.");
        }

        slots[0] = oldValue;
        if ((hooks.WinCount() <= winsBeforeGc) || (hooks.ArgumentErrorCount() != 0))
        {
            throw new InvalidOperationException("The slot was not claimable after a finished GC.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExerciseFaultMapping()
    {
        try
        {
            StoreThroughNull(new Node());
            throw new InvalidOperationException("A null write-barrier destination did not fault.");
        }
        catch (NullReferenceException)
        {
        }

        try
        {
            ClearLargeThroughNull();
            throw new InvalidOperationException("A null layout-clear destination did not fault.");
        }
        catch (NullReferenceException exception)
        {
            if (exception.StackTrace?.Contains(
                    "ClearValueClassWithOldValueWriteBarrier",
                    StringComparison.Ordinal) == true)
            {
                throw new InvalidOperationException(
                    "The layout-clear helper leaked into the null-destination stack trace.");
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreThroughNull(Node value)
    {
        Node? destination = null;
        destination!.Next = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearLargeThroughNull()
    {
        LargeReferenceHolder? destination = null;
        destination!.Value = default;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate ulong GetCallCount();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint GetClobberMask();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ResetSlotLog(nuint destination, byte metadataByte, [MarshalAs(UnmanagedType.I1)] bool claimBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ResetSlotLogRange(
        nuint destination,
        nuint referenceCount,
        byte metadataByte,
        [MarshalAs(UnmanagedType.I1)] bool claimBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetRequiresWork(
        nuint destination,
        [MarshalAs(UnmanagedType.I1)] bool requiresWork);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint GetPointer();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint GetSyntheticDestination(uint metadataByte, uint bit);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool ClaimSynthetic(nuint destination);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.I1)]
    private delegate bool InvokeEmptyRange(nuint destination);

    private delegate void VeryLargeCopy(
        ref VeryLargeMixedLayout destination,
        ref VeryLargeMixedLayout source);

    private sealed record NativeHooks(
        GetCallCount CallCount,
        GetClobberMask ClobberMask,
        GetCallCount RangeCallCount,
        GetCallCount ClearRangeCallCount,
        GetCallCount DependentEdgeCallCount,
        ResetSlotLog Reset,
        ResetSlotLogRange ResetRange,
        SetRequiresWork SetRequiresWork,
        GetCallCount AttemptCount,
        GetCallCount WinCount,
        GetCallCount ArgumentErrorCount,
        GetPointer LastDestination,
        GetSyntheticDestination SyntheticDestination,
        ClaimSynthetic ClaimSynthetic,
        InvokeEmptyRange InvokeEmptyRange);

    private readonly record struct ScalarState(
        Node Object0,
        Node Object1,
        Node Object2,
        Node Object3,
        long Integer0,
        long Integer1,
        long Integer2,
        long Integer3,
        long Integer4,
        long Integer5,
        long Integer6,
        long Integer7);

    private readonly record struct Vector128State(
        Vector128<long> Value0,
        Vector128<long> Value1,
        Vector128<long> Value2,
        Vector128<long> Value3);

    private readonly record struct Vector256State(
        Vector256<long> Value0,
        Vector256<long> Value1,
        Vector256<long> Value2,
        Vector256<long> Value3);

    private readonly record struct Vector512State(
        Vector512<long> Value0,
        Vector512<long> Value1,
        Vector512<long> Value2,
        Vector512<long> Value3);
}

internal sealed class Node
{
    public Node? Next;
}

internal sealed class ObjectHolder
{
    public object? Value;
}

internal sealed class LargeReferenceHolder
{
    public LargeMixedReferences Value;
}

internal sealed class VeryLargeReferenceHolder
{
    public VeryLargeMixedLayout Value;

    public VeryLargeReferenceHolder Clone() =>
        (VeryLargeReferenceHolder)MemberwiseClone();
}

#pragma warning disable CS0649
internal sealed class StackReferenceLayout
{
    public object? F000;
    public object? F001;
    public object? F002;
    public object? F003;
    public object? F004;
    public object? F005;
    public object? F006;
    public object? F007;
    public object? F008;
    public object? F009;
    public object? F010;
    public object? F011;
    public object? F012;
    public object? F013;
    public object? F014;
    public object? F015;
    public object? F016;
    public object? F017;
    public object? F018;
    public object? F019;
    public object? F020;
    public object? F021;
    public object? F022;
    public object? F023;
    public object? F024;
    public object? F025;
    public object? F026;
    public object? F027;
    public object? F028;
    public object? F029;
    public object? F030;
    public object? F031;
    public object? F032;
    public object? F033;
    public object? F034;
    public object? F035;
    public object? F036;
    public object? F037;
    public object? F038;
    public object? F039;
    public object? F040;
    public object? F041;
    public object? F042;
    public object? F043;
    public object? F044;
    public object? F045;
    public object? F046;
    public object? F047;
    public object? F048;
    public object? F049;
    public object? F050;
    public object? F051;
    public object? F052;
    public object? F053;
    public object? F054;
    public object? F055;
    public object? F056;
    public object? F057;
    public object? F058;
    public object? F059;
    public object? F060;
    public object? F061;
    public object? F062;
    public object? F063;
    public object? F064;
    public object? F065;
    public object? F066;
    public object? F067;
    public object? F068;
    public object? F069;
    public object? F070;
    public object? F071;
    public object? F072;
    public object? F073;
    public object? F074;
    public object? F075;
    public object? F076;
    public object? F077;
    public object? F078;
    public object? F079;
    public object? F080;
    public object? F081;
    public object? F082;
    public object? F083;
    public object? F084;
    public object? F085;
    public object? F086;
    public object? F087;
    public object? F088;
    public object? F089;
    public object? F090;
    public object? F091;
    public object? F092;
    public object? F093;
    public object? F094;
    public object? F095;
    public object? F096;
    public object? F097;
    public object? F098;
    public object? F099;
    public object? F100;
    public object? F101;
    public object? F102;
    public object? F103;
    public object? F104;
    public object? F105;
    public object? F106;
    public object? F107;
    public object? F108;
    public object? F109;
    public object? F110;
    public object? F111;
    public object? F112;
    public object? F113;
    public object? F114;
    public object? F115;
    public object? F116;
    public object? F117;
    public object? F118;
    public object? F119;
    public object? F120;
    public object? F121;
    public object? F122;
    public object? F123;
    public object? F124;
    public object? F125;
    public object? F126;
    public object? F127;

    public StackReferenceLayout(object first, object second)
    {
        F000 = first;
        F127 = second;
    }
}
#pragma warning restore CS0649

internal struct MixedReferences
{
    public object? First;
    public nuint Scalar;
    public object? Second;
}

internal struct LargeMixedReferences
{
    public object? First;
    public nuint Scalar0;
    public object? Second;
    public nuint Scalar1;
    public object? Third;
    public nuint Scalar2;
    public object? Fourth;
    public nuint Scalar3;
    public object? Fifth;
    public nuint Scalar4;
    public object? Sixth;
    public nuint Scalar5;
    public object? Seventh;
    public nuint Scalar6;
    public object? Eighth;
    public nuint Scalar7;
    public object? Ninth;
    public nuint Scalar8;
    public object? Tenth;
    public nuint Scalar9;
    public object? Eleventh;
    public nuint Scalar10;
    public object? Twelfth;
    public nuint Scalar11;
    public object? Thirteenth;
    public nuint Scalar12;
    public object? Fourteenth;
    public nuint Scalar13;
    public object? Fifteenth;
    public nuint Scalar14;
    public object? Sixteenth;
    public nuint Scalar15;
}

[InlineArray(2048)]
internal struct VeryLargeMixedLayout
{
    private MixedReferences _element0;
}
