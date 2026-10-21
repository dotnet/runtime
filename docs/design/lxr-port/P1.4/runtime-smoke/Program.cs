// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static class Program
{
    [Flags]
    private enum AllocationFlags : uint
    {
        None = 0,
        ContainsReferences = 1 << 0,
        Finalizable = 1 << 1,
        LargeObjectHeap = 1 << 2,
        PinnedObjectHeap = 1 << 3,
        Array = 1 << 4,
        String = 1 << 5,
        BoxedValueType = 1 << 6,
        Collectible = 1 << 7,
        PayloadMayBeUninitialized = 1 << 8,
        FrozenObjectHeap = 1 << 9,
    }

    private static NativeHooks s_hooks = null!;
    private static object? s_sink;
    private static readonly object s_stackFirst = new();
    private static readonly object s_stackSecond = new();

    private static int Main()
    {
        if (Environment.GetEnvironmentVariable("P14_DEFAULT_SMOKE") == "1")
        {
            s_sink = new PlainObject();
            s_sink = new object[17];
            s_sink = new string('x', 17);
            Console.WriteLine("PASS: default allocation helpers");
            return 0;
        }

        if (Environment.GetEnvironmentVariable("P14_CODEGEN_ONLY") == "1")
        {
            s_sink = AllocatePlainObject();
            s_sink = AllocateIntArray(17);
            s_sink = AllocateString(17);
            Console.WriteLine("PASS: allocation codegen");
            return 0;
        }

        s_hooks = NativeHooks.Load(
            Environment.GetEnvironmentVariable("P14_NATIVE_HOOK_LIBRARY")
                ?? throw new InvalidOperationException("P14_NATIVE_HOOK_LIBRARY is required."));

        ExerciseFrozenRuntimeType();
        ExerciseConstructorOrdering();
        ExercisePlainObject();
        ExerciseArrays();
        ExerciseFailedAllocation();
        ExerciseString();
        ExerciseBoxing();
        ExerciseReflectionAndUninitializedObject();
        ExerciseMemberwiseClone();
        ExerciseFinalizableObject();
        ExerciseLohAndPoh();
        ExerciseLohBoundary();
        ExerciseUninitializedArray();
        ExerciseMultidimensionalArray();
        ExerciseCollectibleType();
        ExerciseMultithreadedUniqueness();
        if (Environment.GetEnvironmentVariable("P14_EXPECT_STACK_ALLOCATION") == "1")
        {
            ExerciseStackAllocationExclusion();
        }

        Console.WriteLine("PASS: exact allocation-complete notifications");
        return 0;
    }

    private static void ExerciseFrozenRuntimeType()
    {
        nint runtimeTypeMethodTable = typeof(Type).GetType().TypeHandle.Value;
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("P14Frozen"),
            AssemblyBuilderAccess.Run);
        ModuleBuilder module = assembly.DefineDynamicModule("P14Frozen");
        TypeBuilder typeBuilder = module.DefineType("FrozenTypeProbe");

        s_hooks.Reset(runtimeTypeMethodTable);
        s_sink = typeBuilder.CreateType();
        VerifyAtLeastOne(
            AllocationFlags.ContainsReferences |
            AllocationFlags.FrozenObjectHeap);
    }

    private static void ExerciseConstructorOrdering()
    {
        s_hooks.Reset(typeof(ConstructorProbe).TypeHandle.Value);
        s_sink = AllocateConstructorProbe();
        VerifySingle(AllocationFlags.None);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ConstructorProbe AllocateConstructorProbe() => new();

    private static void ExercisePlainObject()
    {
        s_hooks.Reset(typeof(PlainObject).TypeHandle.Value);
        s_sink = AllocatePlainObject();
        VerifySingle(AllocationFlags.None);
        Console.WriteLine($"PLAIN_SIZE={s_hooks.GetSize(0)}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static PlainObject AllocatePlainObject() => new();

    private static void ExerciseArrays()
    {
        s_hooks.Reset(typeof(int[]).TypeHandle.Value);
        s_sink = AllocateIntArray(7);
        VerifySingle(AllocationFlags.Array);

        s_hooks.Reset(typeof(object[]).TypeHandle.Value);
        s_sink = AllocateObjectArray(7);
        VerifySingle(AllocationFlags.Array | AllocationFlags.ContainsReferences);
    }

    private static void ExerciseFailedAllocation()
    {
        s_hooks.Reset(typeof(FailureElement[]).TypeHandle.Value);
        try
        {
            s_sink = AllocateFailedArray(-1);
            throw new InvalidOperationException("Negative array allocation unexpectedly succeeded.");
        }
        catch (OverflowException)
        {
        }
        VerifyCount(0);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FailureElement[] AllocateFailedArray(int length) => new FailureElement[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] AllocateIntArray(int length) => new int[length];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object[] AllocateObjectArray(int length) => new object[length];

    private static void ExerciseString()
    {
        s_hooks.Reset(typeof(string).TypeHandle.Value, 40);
        s_sink = AllocateString(7);
        VerifySingle(AllocationFlags.String);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string AllocateString(int length) => new('x', length);

    private static void ExerciseBoxing()
    {
        bool interpreter = Environment.GetEnvironmentVariable("DOTNET_InterpMode") != null;

        s_hooks.Reset(interpreter ? 0 : typeof(int).TypeHandle.Value);
        s_sink = BoxInt(42);
        if (interpreter)
        {
            VerifyAtLeastOne(AllocationFlags.BoxedValueType);
        }
        else
        {
            VerifySingle(AllocationFlags.BoxedValueType);
        }

        int? missing = null;
        s_hooks.Reset(interpreter ? 0 : typeof(int).TypeHandle.Value);
        s_sink = BoxNullable(missing);
        if (interpreter)
        {
            VerifyNoFlag(AllocationFlags.BoxedValueType);
        }
        else
        {
            VerifyCount(0);
        }

        int? present = 42;
        s_hooks.Reset(interpreter ? 0 : typeof(int).TypeHandle.Value);
        s_sink = BoxNullable(present);
        if (interpreter)
        {
            VerifyAtLeastOne(AllocationFlags.BoxedValueType);
        }
        else
        {
            VerifySingle(AllocationFlags.BoxedValueType);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SuppressMessage("Performance", "CA1859", Justification = "Returning object is the operation that forces boxing.")]
    private static object BoxInt(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SuppressMessage("Performance", "CA1859", Justification = "Returning object is the operation that forces nullable boxing.")]
    private static object? BoxNullable(int? value) => value;

    private static void ExerciseReflectionAndUninitializedObject()
    {
        s_hooks.Reset(typeof(ReflectionProbe).TypeHandle.Value);
        s_sink = Activator.CreateInstance(typeof(ReflectionProbe));
        VerifySingle(AllocationFlags.None);

        UninitializedProbe.ConstructorRuns = 0;
        s_hooks.Reset(typeof(UninitializedProbe).TypeHandle.Value);
        s_sink = RuntimeHelpers.GetUninitializedObject(typeof(UninitializedProbe));
        VerifySingle(AllocationFlags.None);
        if (UninitializedProbe.ConstructorRuns != 0)
        {
            throw new InvalidOperationException("GetUninitializedObject ran the constructor.");
        }
    }

    private static void ExerciseMemberwiseClone()
    {
        CloneProbe source = new();
        s_hooks.Reset(typeof(CloneProbe).TypeHandle.Value);
        s_sink = source.Clone();
        VerifySingle(AllocationFlags.None);
    }

    private static void ExerciseFinalizableObject()
    {
        s_hooks.Reset(typeof(FinalizableProbe).TypeHandle.Value);
        FinalizableProbe value = AllocateFinalizable();
        s_sink = value;
        VerifySingle(AllocationFlags.Finalizable);
        GC.SuppressFinalize(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FinalizableProbe AllocateFinalizable() => new();

    private static void ExerciseLohAndPoh()
    {
        s_hooks.Reset(typeof(byte[]).TypeHandle.Value);
        s_sink = AllocateLargeArray();
        VerifySingle(AllocationFlags.Array | AllocationFlags.LargeObjectHeap);

        s_hooks.Reset(typeof(object[]).TypeHandle.Value);
        s_sink = GC.AllocateArray<object>(7, pinned: true);
        VerifySingle(
            AllocationFlags.Array |
            AllocationFlags.ContainsReferences |
            AllocationFlags.PinnedObjectHeap);
    }

    private static void ExerciseLohBoundary()
    {
        s_hooks.Reset(typeof(byte[]).TypeHandle.Value);
        s_sink = AllocateByteArray(84_975);
        VerifySingle(AllocationFlags.Array);

        s_hooks.Reset(typeof(byte[]).TypeHandle.Value);
        s_sink = AllocateByteArray(84_976);
        VerifySingle(AllocationFlags.Array | AllocationFlags.LargeObjectHeap);

        s_hooks.Reset(typeof(byte[]).TypeHandle.Value);
        s_sink = AllocateByteArray(84_977);
        VerifySingle(AllocationFlags.Array | AllocationFlags.LargeObjectHeap);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] AllocateLargeArray() => new byte[100_000];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] AllocateByteArray(int length) => new byte[length];

    private static void ExerciseUninitializedArray()
    {
        s_hooks.Reset(typeof(byte[]).TypeHandle.Value);
        s_sink = GC.AllocateUninitializedArray<byte>(100_000);
        VerifySingle(
            AllocationFlags.Array |
            AllocationFlags.LargeObjectHeap |
            AllocationFlags.PayloadMayBeUninitialized);
    }

    private static void ExerciseMultidimensionalArray()
    {
        s_hooks.Reset(typeof(int[,]).TypeHandle.Value);
        s_sink = Array.CreateInstance(typeof(int), [2, 3], [1, 2]);
        VerifySingle(AllocationFlags.Array);
    }

    private static void ExerciseCollectibleType()
    {
        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("P14Collectible"),
            AssemblyBuilderAccess.RunAndCollect);
        ModuleBuilder module = assembly.DefineDynamicModule("P14Collectible");
        Type type = module.DefineType("CollectibleProbe").CreateType()!;
        nint methodTable = type.TypeHandle.Value;

        s_hooks.Reset(methodTable);
        s_sink = Activator.CreateInstance(type);
        VerifySingle(AllocationFlags.Collectible);
        GC.KeepAlive(assembly);
    }

    private static void ExerciseMultithreadedUniqueness()
    {
        const int ThreadCount = 4;
        const int AllocationsPerThread = 2_000;
        object[][] retained = new object[ThreadCount][];
        Thread[] threads = new Thread[ThreadCount];

        s_hooks.Reset(typeof(PlainObject).TypeHandle.Value);
        for (int threadIndex = 0; threadIndex < ThreadCount; threadIndex++)
        {
            int capturedIndex = threadIndex;
            threads[threadIndex] = new Thread(() =>
            {
                object[] values = new object[AllocationsPerThread];
                for (int allocation = 0; allocation < values.Length; allocation++)
                {
                    values[allocation] = AllocatePlainObject();
                }
                retained[capturedIndex] = values;
            });
            threads[threadIndex].Start();
        }

        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        long expected = ThreadCount * AllocationsPerThread;
        VerifyCount(expected);

        HashSet<nint> objects = new();
        for (long index = 0; index < expected; index++)
        {
            nint address = s_hooks.GetObject(index);
            if ((address == 0) || !objects.Add(address))
            {
                throw new InvalidOperationException($"Duplicate or null notification at index {index}.");
            }
        }

        GC.KeepAlive(retained);
    }

    private static void ExerciseStackAllocationExclusion()
    {
        s_hooks.Reset(typeof(StackProbe).TypeHandle.Value);
        if (!StackAllocate())
        {
            throw new InvalidOperationException("Stack-allocated object lost its field value.");
        }
        VerifyCount(0);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
    private static bool StackAllocate()
    {
        StackProbe value = new(s_stackFirst, s_stackSecond);
        return ReferenceEquals(value.First, s_stackFirst) &&
            ReferenceEquals(value.Second, s_stackSecond);
    }

    private static void VerifySingle(AllocationFlags expectedFlags)
    {
        VerifyCount(1);
        AllocationFlags actualFlags = (AllocationFlags)s_hooks.GetFlags(0);
        if (actualFlags != expectedFlags)
        {
            throw new InvalidOperationException(
                $"Flags mismatch. Expected {expectedFlags}, observed {actualFlags}.");
        }

        nuint size = s_hooks.GetSize(0);
        if ((size == 0) || ((size & (nuint)(IntPtr.Size - 1)) != 0))
        {
            throw new InvalidOperationException($"Invalid aligned size {size}.");
        }
    }

    private static void VerifyAtLeastOne(AllocationFlags expectedFlags)
    {
        long errors = s_hooks.GetErrorCount();
        long count = s_hooks.GetCount();
        if ((errors != 0) || (count == 0))
        {
            throw new InvalidOperationException(
                $"Expected at least one notification and no errors; observed count={count}, errors={errors}.");
        }

        for (long index = 0; index < count; index++)
        {
            if ((AllocationFlags)s_hooks.GetFlags(index) == expectedFlags)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            $"None of {count} notifications carried expected flags {expectedFlags}.");
    }

    private static void VerifyNoFlag(AllocationFlags unexpectedFlag)
    {
        long errors = s_hooks.GetErrorCount();
        long count = s_hooks.GetCount();
        if (errors != 0)
        {
            throw new InvalidOperationException($"Observed {errors} callback argument errors.");
        }

        for (long index = 0; index < count; index++)
        {
            AllocationFlags flags = (AllocationFlags)s_hooks.GetFlags(index);
            if ((flags & unexpectedFlag) != 0)
            {
                throw new InvalidOperationException(
                    $"Notification {index} unexpectedly carried {unexpectedFlag}.");
            }
        }
    }

    private static void VerifyCount(long expected)
    {
        long errors = s_hooks.GetErrorCount();
        long count = s_hooks.GetCount();
        if ((errors != 0) || (count != expected))
        {
            string records = string.Join(
                "; ",
                Enumerable.Range(0, (int)Math.Min(count, 8))
                    .Select(index => $"#{index}:size={s_hooks.GetSize(index)},flags={(AllocationFlags)s_hooks.GetFlags(index)}"));
            throw new InvalidOperationException(
                $"Expected {expected} notifications and no errors; observed count={count}, errors={errors}. {records}");
        }
    }

    private sealed class ConstructorProbe
    {
        public ConstructorProbe()
        {
            if (s_hooks.GetCount() != 1)
            {
                throw new InvalidOperationException("Constructor ran before allocation notification.");
            }
        }
    }

    private sealed class PlainObject
    {
        public long First { get; set; }
        public long Second { get; set; }
    }

    private sealed class ReflectionProbe;

    private sealed class UninitializedProbe
    {
        public static int ConstructorRuns;

        public UninitializedProbe()
        {
            ConstructorRuns++;
        }
    }

    private sealed class CloneProbe
    {
        public CloneProbe Clone() => (CloneProbe)MemberwiseClone();
    }

    private sealed class FinalizableProbe
    {
        public static int Finalized;

        ~FinalizableProbe()
        {
            Interlocked.Increment(ref Finalized);
        }
    }

    private readonly struct FailureElement;

    private sealed class StackProbe
    {
        public object First;
        public object Second;

        public StackProbe(object first, object second)
        {
            First = first;
            Second = second;
        }
    }

    private sealed class NativeHooks
    {
        private readonly nint _library;
        private readonly ResetDelegate _reset;
        private readonly GetInt64Delegate _getCount;
        private readonly GetInt64Delegate _getErrorCount;
        private readonly GetObjectDelegate _getObject;
        private readonly GetSizeDelegate _getSize;
        private readonly GetFlagsDelegate _getFlags;

        private NativeHooks(nint library)
        {
            _library = library;
            _reset = GetDelegate<ResetDelegate>("GC_AllocationNotificationTest_Reset");
            _getCount = GetDelegate<GetInt64Delegate>("GC_AllocationNotificationTest_GetCount");
            _getErrorCount = GetDelegate<GetInt64Delegate>("GC_AllocationNotificationTest_GetErrorCount");
            _getObject = GetDelegate<GetObjectDelegate>("GC_AllocationNotificationTest_GetObject");
            _getSize = GetDelegate<GetSizeDelegate>("GC_AllocationNotificationTest_GetSize");
            _getFlags = GetDelegate<GetFlagsDelegate>("GC_AllocationNotificationTest_GetFlags");
        }

        public static NativeHooks Load(string path) => new(NativeLibrary.Load(path));

        public void Reset(nint methodTable, nuint alignedSize = 0) => _reset(methodTable, alignedSize);

        public long GetCount() => _getCount();

        public long GetErrorCount() => _getErrorCount();

        public nint GetObject(long index) => _getObject(index);

        public nuint GetSize(long index) => _getSize(index);

        public uint GetFlags(long index) => _getFlags(index);

        private T GetDelegate<T>(string name) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ResetDelegate(nint methodTable, nuint alignedSize);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate long GetInt64Delegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nint GetObjectDelegate(long index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate nuint GetSizeDelegate(long index);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint GetFlagsDelegate(long index);
    }
}
