// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace Webcil;

public static class WasmWebcilModule
{
    public static string FixupLeaf() => "transitive-precode-string";

    public static void StoreReference(ref object location, object value)
    {
        location = value;
    }

    public struct ReferencePair
    {
        public object First;
        public object Second;
    }

    public static void CopyReferences(ref ReferencePair destination, ref ReferencePair source)
    {
        destination = source;
    }

    public static int DeferredCallArgumentOrder(DeferredCallReceiver receiver, int value)
    {
        return receiver.Read(AddIntegers(value, value), AddIntegers(value + 1, value)) + 1;
    }

    public sealed class DeferredCallReceiver
    {
        public int Value;

        public int Read(int first, int second)
        {
            return Value + first + second;
        }
    }

    public static string FixupMiddle(bool useCallee)
    {
        string value = FixupLeaf();
        return useCallee ? value : "transitive-precode-string";
    }

    public static string FixupOuter(bool useCallee)
    {
        string value = FixupMiddle(useCallee);
        return useCallee ? value : "transitive-precode-string";
    }

    public static string FixupAtDepthLimit(bool useCallee)
    {
        string value = FixupOuter(useCallee);
        return useCallee ? value : "transitive-precode-string";
    }

    public static string FixupBeyondDepthLimit(bool useCallee)
    {
        string value = FixupAtDepthLimit(useCallee);
        return useCallee ? value : "transitive-precode-string";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string ColdFixup(bool useCallee) => FixupOuter(useCallee);

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static string UnoptimizedFixup(bool useCallee) => FixupOuter(useCallee);

    public static string HotFixupLeaf(bool useCold) => useCold ? ColdFixup(useCold) : "hot-precode-string";

    public static string UnoptimizedFixupLeaf(bool useCold) => useCold ? UnoptimizedFixup(useCold) : "hot-precode-string";

    public static string HotFixupCaller(bool useCold) => HotFixupLeaf(useCold);

    public static string UnoptimizedFixupCaller(bool useCold) => UnoptimizedFixupLeaf(useCold);

    public static string CollectionDepthExemptCaller(bool useCallee)
    {
        string value = FixupCollection.HighLevelCollection(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public static string EnumerableDepthExemptCaller(bool useCallee)
    {
        string value = FixupEnumerable.HighLevelEnumerable(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public static string InheritedEnumerableDepthExemptCaller(bool useCallee)
    {
        string value = InheritedFixupEnumerable.HighLevelInheritedEnumerable(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public static string EnumeratorDepthExemptCaller(bool useCallee)
    {
        string value = FixupEnumerator.HighLevelEnumerator(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public static string ColdCollectionDepthExemptCaller(bool useCallee)
    {
        string value = FixupCollection.ColdCollection(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public static string UnoptimizedCollectionDepthExemptCaller(bool useCallee)
    {
        string value = FixupCollection.UnoptimizedCollection(useCallee);
        return useCallee ? value : "depth-exempt-string";
    }

    public sealed class FixupCollection : ICollection
    {
        public int Count => 0;
        public bool IsSynchronized => false;
        public object SyncRoot => this;
        public void CopyTo(Array array, int index) { }
        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        public static string HighLevelCollection(bool useCallee) =>
            useCallee ? FixupAtDepthLimit(useCallee) : "depth-exempt-string";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string ColdCollection(bool useCallee) => HighLevelCollection(useCallee);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public static string UnoptimizedCollection(bool useCallee) => HighLevelCollection(useCallee);
    }

    public class FixupEnumerable : IEnumerable<int>
    {
        public IEnumerator<int> GetEnumerator() => ((IEnumerable<int>)Array.Empty<int>()).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public static string HighLevelEnumerable(bool useCallee) =>
            useCallee ? FixupAtDepthLimit(useCallee) : "depth-exempt-string";
    }

    public sealed class InheritedFixupEnumerable : FixupEnumerable
    {
        public static string HighLevelInheritedEnumerable(bool useCallee) =>
            useCallee ? FixupAtDepthLimit(useCallee) : "depth-exempt-string";
    }

    public sealed class FixupEnumerator : IEnumerator<int>
    {
        public int Current => 0;
        object IEnumerator.Current => Current;
        public bool MoveNext() => false;
        public void Reset() { }
        public void Dispose() { }

        public static string HighLevelEnumerator(bool useCallee) =>
            useCallee ? FixupAtDepthLimit(useCallee) : "depth-exempt-string";
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string UncoveredFixup() => "uncovered-precode-string";

    public interface IReceiverMap
    {
        object RoundTrip(object a, object b, object c, object d, object e, object f, object g, object h, ref object reference);
    }

    public struct ReceiverMapValue : IReceiverMap
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public object RoundTrip(object a, object b, object c, object d, object e, object f, object g, object h, ref object reference)
        {
            reference = h;
            return a;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CallBoxedReceiver(IReceiverMap target, object value)
    {
        return target.RoundTrip(value, value, value, value, value, value, value, value, ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object CallUnboxedReceiver(ref ReceiverMapValue target, object value)
    {
        return target.RoundTrip(value, value, value, value, value, value, value, value, ref value);
    }

    public struct SignatureOnlyValue
    {
        public object Reference;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static SignatureOnlyValue SignatureOnlyCallee(SignatureOnlyValue value)
    {
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool CallSignatureOnlyValue()
    {
        return SignatureOnlyCallee(default).Reference is null;
    }

    public sealed class LdtokenType
    {
    }

    public sealed class LdtokenHolder<T>
    {
    }

    public sealed class OtherLdtokenType
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool FoldTypeTokenEquality() => typeof(LdtokenType) == typeof(LdtokenType);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool FoldTypeTokenInequality() => typeof(LdtokenType) == typeof(OtherLdtokenType);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadTypeToken() => typeof(LdtokenType);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static RuntimeTypeHandle ReadTypeTokenHandle() => typeof(LdtokenType).TypeHandle;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadConstructedTypeToken() => typeof(LdtokenHolder<LdtokenType[]>);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadGenericTypeToken<T>() => typeof(T);

    public struct GenericTokenValue
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadIntrinsicTypeToken(int index) => index switch
    {
        0 => typeof(void),
        1 => typeof(bool),
        2 => typeof(char),
        3 => typeof(sbyte),
        4 => typeof(byte),
        5 => typeof(short),
        6 => typeof(ushort),
        7 => typeof(int),
        8 => typeof(uint),
        9 => typeof(long),
        10 => typeof(ulong),
        11 => typeof(float),
        12 => typeof(double),
        13 => typeof(nint),
        14 => typeof(nuint),
        15 => typeof(string),
        16 => typeof(object),
        _ => typeof(TypedReference),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadIntrinsicArrayTypeToken(int index) => index switch
    {
        0 => typeof(bool[]),
        1 => typeof(int[,]),
        2 => typeof(int[][]),
        3 => typeof(string[]),
        4 => typeof(object[,,]),
        5 => typeof(int[,][]),
        6 => typeof(int[]),
        7 => typeof(int[][,]),
        _ => typeof(nuint[]),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Type ReadTokenDependentTypeToken(int index) => index switch
    {
        0 => typeof(GenericTokenValue),
        1 => typeof(LdtokenType),
        2 => typeof(DayOfWeek),
        3 => typeof(decimal),
        4 => typeof(Half),
        5 => typeof(int?),
        6 => typeof(LdtokenHolder<int>),
        7 => typeof(LdtokenType[]),
        _ => typeof(GenericTokenValue[,][]),
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nint ReadIntPtrZero() => IntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nuint ReadUIntPtrZero() => UIntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string ReadStringEmpty() => string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ReadIsLittleEndian() => BitConverter.IsLittleEndian;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly nint AddressIntPtrZero() => ref IntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly nuint AddressUIntPtrZero() => ref UIntPtr.Zero;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly string AddressStringEmpty() => ref string.Empty;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static ref readonly bool AddressIsLittleEndian() => ref BitConverter.IsLittleEndian;

    private static readonly int[] s_primes = new int[] { 3, 5, 7, 11, 13 };
    private static int s_counter;

    private sealed class GcMarker
    {
        public readonly int Value;

        public GcMarker(int value)
        {
            Value = value;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AddIntegers(int left, int right)
    {
        return left + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FoldableBodyOne()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void FoldableBodyTwo()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int OptimizedTrackedVariables(int left, int right)
    {
        int sum = left + right;
        if (sum < 0)
        {
            return AddIntegers(sum, right);
        }

        int difference = left - right;
        return AddIntegers(sum, difference);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> OptimizedVectorVariables(Vector128<int> left, Vector128<int> right)
    {
        Vector128<int> sum = left + right;
        if (sum.GetElement(0) < 0)
        {
            return AddVectors(sum, right);
        }

        Vector128<int> difference = left - right;
        return AddVectors(sum, difference);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector128<int> AddVectors(Vector128<int> left, Vector128<int> right)
    {
        return left + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LeafFrameLocal(int value)
    {
        int local = value + 1;
        int* address = &local;
        return *address * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LocallocFrameLocal(int value)
    {
        int length = (value & 3) + 1;
        int* storage = stackalloc int[length];
        *storage = value;
        return *storage;
    }

    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static double AddDoubles(double left, double right)
    {
        double result = left + right;
        return result;
    }

    // Explicit construction avoids distinct compiler-generated delegate cache fields.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MethodInfo SelfMethodOne() => new Func<MethodInfo>(SelfMethodOne).Method;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static MethodInfo SelfMethodTwo() => new Func<MethodInfo>(SelfMethodTwo).Method;

    // Reads static data, which forces the JIT to materialize the imageBase address via a
    // 'global.get' of the wasm imageBase well-known global.
    public static int SumStaticData(int index)
    {
        s_counter += 1;
        return AddIntegers(s_primes[index], s_counter);
    }

    // An unoptimized try/finally makes the JIT emit a call to the 'finally'
    // funclet (genCallFinally), which computes the funclet's address from the
    // wasm table-base well-known global via a 'global.get'. Like the image
    // base, that table-base global is referenced through a
    // WASM_GLOBAL_INDEX_LEB relocation the R2R object writer must self-resolve
    // back to the fixed table-base global index; if that resolution regresses,
    // the emitted 'global.get' encoding changes (or crossgen2 throws while
    // emitting this method).
    [MethodImpl(MethodImplOptions.NoOptimization)]
    public static int SumWithFinally(int index)
    {
        int total = 0;
        try
        {
            total = s_primes[index];
        }
        finally
        {
            s_counter++;
        }
        return total + s_counter;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int GcLocalAcrossFinally(int value)
    {
        GcMarker marker = new(value);
        try
        {
            return marker.Value;
        }
        finally
        {
            // Keep the newly allocated object live in the parent frame across an actual GC
            // reached through the finally funclet.
            CollectAtGcSafepoint();
            GC.KeepAlive(marker);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void CollectAtGcSafepoint()
    {
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static unsafe int LocallocAcrossFinally(int value)
    {
        int length = (value & 3) + 1;
        int* storage = stackalloc int[length];
        *storage = value;
        try
        {
            return *storage;
        }
        finally
        {
            CollectAtGcSafepoint();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int GcSlotIdentity()
    {
        GcMarker first = new(17);
        if (first.Value == 0)
        {
            return 0;
        }

        GcMarker second = new(29);
        if (second.Value == 0)
        {
            return 0;
        }

        GcMarker? empty = null;
        TouchMarkerSlot(ref empty);

        CollectAtGcSafepoint();
        int result = (first.Value * 100) + second.Value;
        GC.KeepAlive(first);
        GC.KeepAlive(second);
        GC.KeepAlive(empty);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TouchMarkerSlot(ref GcMarker? marker)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CatchExceptionOne(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static int CatchExceptionTwo(int value)
    {
        try
        {
            return 100 / value;
        }
        catch (DivideByZeroException)
        {
            return -1;
        }
    }
}

public class WasmSelfForwarder
{
    private readonly WasmSelfForwarder _target;

    public WasmSelfForwarder(WasmSelfForwarder? target = null)
    {
        _target = target ?? this;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int BeforeEvent(int value) => _target.BeforeEvent(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int AfterEvent(int value) => _target.AfterEvent(value);
}
