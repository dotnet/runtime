// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

internal enum ScalarEnum : short { MinusOne = -1, Zero = 0, One = 1 }
internal enum MathOperation { Abs, Sqrt, Floor, Ceiling, Truncate, CopySign, Identity }

internal static unsafe class ManagedExports
{
    // Integer operations.

    [UnmanagedCallersOnly(EntryPoint = "ManagedAdd", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Add(int x, int y) => unchecked(x + y);

    [UnmanagedCallersOnly(EntryPoint = "ManagedSubtract", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Subtract(int x, int y) => unchecked(x - y);

    [UnmanagedCallersOnly(EntryPoint = "ManagedWide", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static long Wide(long x, long y) => unchecked((x - y) ^ (x * y));

    [UnmanagedCallersOnly(EntryPoint = "ManagedShift", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static long Shift(long x, int count) => (x << count) ^ (x >> count) ^ (long)((ulong)x >> count);

    [UnmanagedCallersOnly(EntryPoint = "ManagedConvert", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static long Convert(int x) => (long)x + (long)(uint)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedCompare", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Compare(ulong x, ulong y) => x > y ? 1 : 0;

    [UnmanagedCallersOnly(EntryPoint = "ManagedLocals", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Locals(int x, int y)
    {
        int saved = unchecked(x * y);
        x = ~x;
        return unchecked(saved + x - y);
    }

    [UnmanagedCallersOnly(EntryPoint = "ManagedBranch", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Branch(int x) => x < 0 ? -x : x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedPhi", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Phi(int x, int y) => unchecked((x > y ? x : y) + (x < 0 ? 7 : 11));

    [UnmanagedCallersOnly(EntryPoint = "ManagedSwitch", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Switch(int x)
    {
        switch (x)
        {
            case 0: return 71;
            case 1: return 13;
            case 2: return -37;
            case 3: return 5;
            case 4: return 101;
            case 5: return -9;
            default: return unchecked(x + 19);
        }
    }

    [UnmanagedCallersOnly(EntryPoint = "ManagedNarrow", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static byte Narrow(int x) => unchecked((byte)(x + 129));

    [UnmanagedCallersOnly(EntryPoint = "ManagedSmallArguments", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static short SmallArguments(sbyte x, ushort y, ushort z, byte flag) => unchecked((short)(flag != 0 ? x + y + z : x - y - z));

    [UnmanagedCallersOnly(EntryPoint = "ManagedEnum", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static ScalarEnum Enum(ScalarEnum x) => unchecked((ScalarEnum)((short)x + 1));

    [UnmanagedCallersOnly(EntryPoint = "ManagedNativeInt", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static nint NativeInt(nint x, int count) => ((x + 5) >> count) ^ (nint)(uint)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedSafeDivide", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int SafeDivide(int x) => x / 3;

    [UnmanagedCallersOnly(EntryPoint = "ManagedSafeRemainder", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static ulong SafeRemainder(ulong x) => x % 7;

    [UnmanagedCallersOnly(EntryPoint = "ManagedLocalAddress", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int LocalAddress(int x, int y)
    {
        int value = x;
        ref int reference = ref value;
        reference = unchecked(reference + y);
        return value ^ x;
    }

    // Floating-point operations.

    [UnmanagedCallersOnly(EntryPoint = "ManagedFloat", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static double Floating(double x) => x + 1;

    [UnmanagedCallersOnly(EntryPoint = "ManagedFloatArithmetic", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static float FloatArithmetic(float x, float y) => (x + y) - x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleArithmetic", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static double DoubleArithmetic(double x, double y) => (x - y) * 2.0 / y;

    [UnmanagedCallersOnly(EntryPoint = "ManagedFloatCompare", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int FloatCompare(double x, double y) => x < y ? 1 : x > y ? 2 : x == y ? 3 : 4;

    [UnmanagedCallersOnly(EntryPoint = "ManagedUIntToDouble", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static double UIntToDouble(ulong x) => x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToInt", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int DoubleToInt(double x) => (int)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToUInt", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static uint DoubleToUInt(double x) => (uint)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToLong", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static long DoubleToLong(double x) => (long)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToULong", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static ulong DoubleToULong(double x) => (ulong)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToByte", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static byte DoubleToByte(double x) => (byte)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedDoubleToSByte", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static sbyte DoubleToSByte(double x) => (sbyte)x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedLocalBitcast", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int LocalBitcast(float x) => *(int*)&x;

    [UnmanagedCallersOnly(EntryPoint = "ManagedMath", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static double MathIntrinsic(double x, double y, MathOperation operation) => operation switch
    {
        MathOperation.Abs => Math.Abs(x),
        MathOperation.Sqrt => Math.Sqrt(x),
        MathOperation.Floor => Math.Floor(x),
        MathOperation.Ceiling => Math.Ceiling(x),
        MathOperation.Truncate => Math.Truncate(x),
        MathOperation.CopySign => Math.CopySign(x, y),
        _ => x,
    };

    [UnmanagedCallersOnly(EntryPoint = "ManagedMathF", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static float MathFIntrinsic(float x, float y, MathOperation operation) => operation switch
    {
        MathOperation.Abs => MathF.Abs(x),
        MathOperation.Sqrt => MathF.Sqrt(x),
        MathOperation.Floor => MathF.Floor(x),
        MathOperation.Ceiling => MathF.Ceiling(x),
        MathOperation.Truncate => MathF.Truncate(x),
        MathOperation.CopySign => MathF.CopySign(x, y),
        _ => x,
    };

    // Calls and function pointers.

    [UnmanagedCallersOnly(EntryPoint = "ManagedCompose", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Compose(int x) => ManagedHelpers.Add(x, 2) ^ ManagedHelpers.Add(x, -3);

    [UnmanagedCallersOnly(EntryPoint = "ManagedPointerGetter", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static delegate* unmanaged[Cdecl]<int, int, int> PointerGetter() => &ManagedExports.Add;

    [UnmanagedCallersOnly(EntryPoint = "ManagedPointerSelect", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static delegate* unmanaged[Cdecl]<int, int, int> PointerSelect(delegate* unmanaged[Cdecl]<int, int, int> left, delegate* unmanaged[Cdecl]<int, int, int> right, int choose) => choose == 0 ? left : right;

    [UnmanagedCallersOnly(EntryPoint = "ManagedKnownCalli", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int KnownCalli(int x)
    {
        delegate*<int, int, int> callback = &ManagedHelpers.Add;
        return callback(x, 1);
    }

    [UnmanagedCallersOnly(EntryPoint = "ManagedVoid")]
    public static void Empty() { }

    [UnmanagedCallersOnly(EntryPoint = "ManagedCallback", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Callback(int x) => unchecked(x + 9);

    [UnmanagedCallersOnly(EntryPoint = "ManagedQuoted%arg0", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int QuotedAdd(int x, int y) => unchecked(x + y);

    [UnmanagedCallersOnly(EntryPoint = "ManagedQuotedPointerGetter", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static delegate* unmanaged[Cdecl]<int, int, int> QuotedPointerGetter() => ManagedHelpers.GetQuotedPointer();
}

internal static unsafe class ManagedHelpers
{
    // Exercise LLVM inlining of nested calls, multiple returns, and quoted symbols.
    public static int Add(int x, int y)
    {
        if (x < 0)
            return unchecked(x + y);
        if (x == 0)
            return y;
        return AddPositive(x, y);
    }

    public static int AddPositive(int x, int y) => unchecked(x + y);
    public static delegate* unmanaged[Cdecl]<int, int, int> GetQuotedPointer() => &ManagedExports.QuotedAdd;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int NoInline(int x) => unchecked(x + 7);
    public static int Recursive(int x) => x == 0 ? 0 : Recursive(x - 1);
}

internal static unsafe class RejectedExports
{
    public static int Field;

    [UnmanagedCallersOnly(EntryPoint = "RejectedNoInline", CallConvs = new[] { typeof(CallConvCdecl) }), MethodImpl(MethodImplOptions.NoInlining)]
    public static int NoInline(int x) => unchecked(x + 7);

    [UnmanagedCallersOnly(EntryPoint = "RejectedNoOptimization", CallConvs = new[] { typeof(CallConvCdecl) }), MethodImpl(MethodImplOptions.NoOptimization)]
    public static int NoOptimization(int x) => unchecked(x + 7);

    [UnmanagedCallersOnly(EntryPoint = "RejectedCall", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Call(int x) => ManagedHelpers.NoInline(x);

    [UnmanagedCallersOnly(EntryPoint = "RejectedField", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int ReadField() => Field;

    [UnmanagedCallersOnly(EntryPoint = "RejectedChecked", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Checked(int x) => checked(x + 1);

    [UnmanagedCallersOnly(EntryPoint = "RejectedDivision", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Divide(int x, int y) => x / y;

    [UnmanagedCallersOnly(EntryPoint = "RejectedReference", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Reference(nint x) { object value = x == 0 ? null : new object(); return value == null ? 0 : 1; }

    [UnmanagedCallersOnly(EntryPoint = "RejectedFunctionPointerCall", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe int Indirect(delegate* unmanaged[Cdecl]<int, int, int> callback, int x, int y) => callback(x, y);

    [UnmanagedCallersOnly(EntryPoint = "RejectedPointerRead", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int PointerRead(int* pointer) => *pointer;

    [UnmanagedCallersOnly(EntryPoint = "RejectedDivideMinusOne", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int DivideMinusOne(int x) => x / -1;

    [UnmanagedCallersOnly(EntryPoint = "RejectedLoop", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Loop(int count) { int sum = 0; for (int i = 0; i < count; i++) sum += i; return sum; }

    [UnmanagedCallersOnly(EntryPoint = "RejectedRecursive", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Recursive(int x) => ManagedHelpers.Recursive(x);

    [UnmanagedCallersOnly(EntryPoint = "RejectedEH", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int ExceptionHandling(int x) { try { return x; } finally { RejectedExports.Field = x; } }
}

internal static class TypeWithConstructor
{
    public static int Field;
    static TypeWithConstructor() { Field = 23; }

    [UnmanagedCallersOnly(EntryPoint = "RejectedClassConstructor", CallConvs = new[] { typeof(CallConvCdecl) })]
    public static int Leaf(int x) => unchecked(x + 1);
}

internal static class PreinitScalarFixture
{
    public static readonly long Value = Compute();
    public static readonly int Comparison = Compare(double.NaN, -1.0);
    private static long Compute()
    {
        long wide = 0;
        int narrow = 0;
        for (int i = 1; i < 12; i++)
        {
            narrow = unchecked((narrow + i) * 3 - i);
            wide += (narrow < 0 ? -1L : narrow) / 3L;
            wide = (wide << 1) | (uint)(narrow & 255);
        }
        return wide;
    }
    private static int Compare(double x, double y) => x < y ? 1 : x >= y ? 2 : 3;
}

internal static unsafe class LlvmLtoTests
{
    // Integer operations.
    [DllImport("llvmltonative", EntryPoint = "NativeWide", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeWide(long x, long y);
    [DllImport("llvmltonative", EntryPoint = "NativeShift", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeShift(long x, int count);
    [DllImport("llvmltonative", EntryPoint = "NativeConvert", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeConvert(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeCompare", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeCompare(ulong x, ulong y);
    [DllImport("llvmltonative", EntryPoint = "NativeLocals", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeLocals(int x, int y);
    [DllImport("llvmltonative", EntryPoint = "NativePhi", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativePhi(int x, int y);
    [DllImport("llvmltonative", EntryPoint = "NativeSwitch", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeSwitch(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeNarrow", CallingConvention = CallingConvention.Cdecl)]
    private static extern byte NativeNarrow(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeSmallArguments", CallingConvention = CallingConvention.Cdecl)]
    private static extern short NativeSmallArguments(sbyte x, ushort y, ushort z, byte flag);
    [DllImport("llvmltonative", EntryPoint = "NativeEnum", CallingConvention = CallingConvention.Cdecl)]
    private static extern ScalarEnum NativeEnum(ScalarEnum x);
    [DllImport("llvmltonative", EntryPoint = "NativeNativeInt", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeNativeInt(nint x, int count);
    [DllImport("llvmltonative", EntryPoint = "NativeSafeDivide", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeSafeDivide(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeSafeRemainder", CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong NativeSafeRemainder(ulong x);
    [DllImport("llvmltonative", EntryPoint = "NativeLocalAddress", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeLocalAddress(int x, int y);

    // Floating-point operations.
    [DllImport("llvmltonative", EntryPoint = "NativeFloatArithmetic", CallingConvention = CallingConvention.Cdecl)]
    private static extern float NativeFloatArithmetic(float x, float y);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleArithmetic", CallingConvention = CallingConvention.Cdecl)]
    private static extern double NativeDoubleArithmetic(double x, double y);
    [DllImport("llvmltonative", EntryPoint = "NativeFloatCompare", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeFloatCompare(double x, double y);
    [DllImport("llvmltonative", EntryPoint = "NativeUIntToDouble", CallingConvention = CallingConvention.Cdecl)]
    private static extern double NativeUIntToDouble(ulong x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToInt", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeDoubleToInt(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToUInt", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint NativeDoubleToUInt(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToLong", CallingConvention = CallingConvention.Cdecl)]
    private static extern long NativeDoubleToLong(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToULong", CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong NativeDoubleToULong(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToByte", CallingConvention = CallingConvention.Cdecl)]
    private static extern byte NativeDoubleToByte(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeDoubleToSByte", CallingConvention = CallingConvention.Cdecl)]
    private static extern sbyte NativeDoubleToSByte(double x);
    [DllImport("llvmltonative", EntryPoint = "NativeLocalBitcast", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeLocalBitcast(float x);
    [DllImport("llvmltonative", EntryPoint = "NativeMath", CallingConvention = CallingConvention.Cdecl)]
    private static extern double NativeMath(double x, double y, MathOperation operation);
    [DllImport("llvmltonative", EntryPoint = "NativeMathF", CallingConvention = CallingConvention.Cdecl)]
    private static extern float NativeMathF(float x, float y, MathOperation operation);

    // Calls and function pointers.
    [DllImport("llvmltonative", EntryPoint = "NativeFold", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeFold(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeCompose", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeCompose(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeVoid", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeVoid();
    [DllImport("llvmltonative", EntryPoint = "NativeNoInline", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeNoInline(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeKnownPointer", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeKnownPointer(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeRuntimePointer", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeRuntimePointer(nint callback, int x, int y);
    [DllImport("llvmltonative", EntryPoint = "NativeGetPointer", CallingConvention = CallingConvention.Cdecl)]
    private static extern nint NativeGetPointer();
    [DllImport("llvmltonative", EntryPoint = "NativeThroughManagedPointer", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeThroughManagedPointer(int x);
    [DllImport("llvmltonative", EntryPoint = "NativePointerSelect", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativePointerSelect(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeKnownCalli", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeKnownCalli(int x);

    // Unmanaged callbacks retain both forward and reverse transitions.
    [DllImport("llvmltonative", EntryPoint = "NativeCallback", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeCallback(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeCallbackPointer", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeCallbackPointer(nint callback, int x);

    [DllImport("llvmltonative", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeRejectedIndirect(nint callback, int x, int y);
    [DllImport("llvmltonative", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeClassConstructor(int x);
    [DllImport("llvmltonative", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeQuotedPointer(int x);
    [DllImport("llvmltonative", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeTransitionStress(int count);
    [DllImport("llvmltonative", CallingConvention = CallingConvention.Cdecl)]
    private static extern int NativeForeignThread(int x, int y);

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ManagedHelpers))]
    private static int Main()
    {
        VerifyLlvmLto();
        int[] integers = [ int.MinValue, int.MaxValue, -65537, -256, -129, -100, -1, 0, 1, 2, 3, 4, 5, 6,
            20, 31, 32, 63, 64, 100, 127, 255, 65535 ];
        long[] wideIntegers = [long.MinValue, long.MaxValue, -4294967296L, -1, 0, 1, 4294967296L];

        int result = TestIntegers(integers, wideIntegers);
        if (result != 100)
            return result;
        result = TestFloatingPoint(integers);
        if (result != 100)
            return result;
        result = TestCallsAndFunctionPointers(integers);
        if (result != 100)
            return result;
        return TestPreinitialization();
    }

    private static int TestIntegers(int[] integers, long[] wideIntegers)
    {
        foreach (int x in integers)
        {
            if (NativeConvert(x) != (long)x + (long)(uint)x) return 1;
            if (NativeSwitch(x) != (x switch { 0 => 71, 1 => 13, 2 => -37, 3 => 5, 4 => 101, 5 => -9, _ => unchecked(x + 19) })) return 101;
            if (NativeNarrow(x) != unchecked((byte)(x + 129))) return 102;
            if (NativeEnum((ScalarEnum)x) != unchecked((ScalarEnum)((short)x + 1))) return 103;
            if (NativeSafeDivide(x) != x / 3) return 108;
            if (NativeSafeRemainder((ulong)(long)x) != (ulong)(long)x % 7) return 109;
            foreach (int y in integers)
            {
                if (NativeLocals(x, y) != unchecked(x * y + ~x - y))
                    return 2;
                if (NativePhi(x, y) != unchecked((x > y ? x : y) + (x < 0 ? 7 : 11)) ||
                    NativeLocalAddress(x, y) != (unchecked(x + y) ^ x) ||
                    NativeNativeInt((nint)(long)x, y) != ((((nint)(long)x + 5) >> y) ^ (nint)(uint)x) ||
                    NativeSmallArguments((sbyte)x, (ushort)y, (ushort)x, (byte)((y & 1) == 0 ? 1 : 0)) !=
                        unchecked((short)((y & 1) == 0 ? (sbyte)x + (ushort)y + (char)x : (sbyte)x - (ushort)y - (char)x)))
                    return 11;
            }
        }
        foreach (long x in wideIntegers)
        {
            foreach (long y in wideIntegers)
                if (NativeWide(x, y) != unchecked((x - y) ^ (x * y)) ||
                    NativeCompare((ulong)x, (ulong)y) != ((ulong)x > (ulong)y ? 1 : 0))
                    return 3;
            foreach (int count in integers)
                if (NativeShift(x, count) != ((x << count) ^ (x >> count) ^ (long)((ulong)x >> count)))
                    return 4;
        }
        return 100;
    }

    private static bool Same(double x, double y) => double.IsNaN(x) && double.IsNaN(y) || BitConverter.DoubleToInt64Bits(x) == BitConverter.DoubleToInt64Bits(y);
    private static bool Same(float x, float y) => float.IsNaN(x) && float.IsNaN(y) || BitConverter.SingleToInt32Bits(x) == BitConverter.SingleToInt32Bits(y);

    private static int TestFloatingPoint(int[] integers)
    {
        double[] doubles = [ double.NaN, double.NegativeInfinity, double.PositiveInfinity, -1e30, 1e30, -0.0, 0.0, -1.5, 1.5,
            -129, 128, 256, 32768, 2147483648.0, 4294967296.0, 9223372036854775808.0, 18446744073709551616.0 ];
        float[] singles = [float.NaN, float.NegativeInfinity, float.PositiveInfinity, float.MaxValue, float.Epsilon, -0.0f, 0.0f, -1.5f, 1.5f, 16777216f];
        // Enumerate named cases explicitly so these checks do not depend on enum metadata.
        MathOperation[] mathOperations = { MathOperation.Abs, MathOperation.Sqrt, MathOperation.Floor,
            MathOperation.Ceiling, MathOperation.Truncate, MathOperation.CopySign, MathOperation.Identity };

        foreach (int x in integers)
            if (!Same(NativeUIntToDouble((ulong)(long)x), (double)(ulong)(long)x))
                return 12;
        foreach (double x in doubles)
        {
            if (NativeDoubleToInt(x) != (int)x) return 201;
            if (NativeDoubleToUInt(x) != (uint)x) return 202;
            if (NativeDoubleToLong(x) != (long)x) return 203;
            if (NativeDoubleToULong(x) != (ulong)x) return 204;
            if (NativeDoubleToByte(x) != (byte)x) return 205;
            if (NativeDoubleToSByte(x) != (sbyte)x) return 206;
            foreach (MathOperation operation in mathOperations)
            {
                double expectedMath = operation switch
                {
                    MathOperation.Abs => Math.Abs(x),
                    MathOperation.Sqrt => Math.Sqrt(x),
                    MathOperation.Floor => Math.Floor(x),
                    MathOperation.Ceiling => Math.Ceiling(x),
                    MathOperation.Truncate => Math.Truncate(x),
                    MathOperation.CopySign => Math.CopySign(x, -0.0),
                    _ => x,
                };
                if (!Same(NativeMath(x, -0.0, operation), expectedMath)) return 207;
            }
            foreach (double y in doubles)
                if (!Same(NativeDoubleArithmetic(x, y), (x - y) * 2.0 / y) || NativeFloatCompare(x, y) != (x < y ? 1 : x > y ? 2 : x == y ? 3 : 4))
                    return 14;
        }
        foreach (float x in singles)
        {
            if (NativeLocalBitcast(x) != BitConverter.SingleToInt32Bits(x))
                return 15;
            foreach (float y in singles)
                if (!Same(NativeFloatArithmetic(x, y), (x + y) - x))
                    return 16;
            foreach (MathOperation operation in mathOperations)
            {
                float expectedMath = operation switch
                {
                    MathOperation.Abs => MathF.Abs(x),
                    MathOperation.Sqrt => MathF.Sqrt(x),
                    MathOperation.Floor => MathF.Floor(x),
                    MathOperation.Ceiling => MathF.Ceiling(x),
                    MathOperation.Truncate => MathF.Truncate(x),
                    MathOperation.CopySign => MathF.CopySign(x, -0.0f),
                    _ => x,
                };
                if (!Same(NativeMathF(x, -0.0f, operation), expectedMath)) return 208;
            }
        }
        return 100;
    }

    private static int TestCallsAndFunctionPointers(int[] integers)
    {
        RejectedExports.Field = 17;
        delegate* unmanaged[Cdecl]<int, int, int> addPointer = &ManagedExports.Add;
        delegate* unmanaged[Cdecl]<int, int, int> subtractPointer = &ManagedExports.Subtract;
        delegate* unmanaged[Cdecl]<int, int> unmanagedPointer = &ManagedExports.Callback;
        // Check actual code addresses in this build with method folding disabled.
        if (NativeGetPointer() != (nint)addPointer || (nint)addPointer == (nint)subtractPointer)
            return 6;

        foreach (int x in integers)
        {
            if (NativeFold(x) != (unchecked(x + 1) ^ 0x1234) ||
                NativeNoInline(x) != unchecked(x + 7) ||
                NativeCallback(x) != unchecked(x + 9))
                return 1;
            if (NativeCompose(x) != (unchecked(x + 2) ^ unchecked(x - 3))) return 104;
            if (NativeThroughManagedPointer(x) != unchecked(x + 1)) return 105;
            if (NativeKnownCalli(x) != unchecked(x + 1)) return 106;
            if (NativeQuotedPointer(x) != unchecked(x + 1)) return 110;
            if (NativePointerSelect(x) != ((x & 1) == 0 ? unchecked(x + 1) : unchecked(x - 1))) return 107;

            delegate* unmanaged[Cdecl]<int, int, int> selectedPointer = (x & 1) == 0 ? addPointer : subtractPointer;
            int expectedIndirect = (x & 1) == 0 ? unchecked(x + 1) : unchecked(x - 1);
            if (NativeKnownPointer(x) != (unchecked(x + 1) ^ 0x1234) ||
                NativeRuntimePointer((nint)selectedPointer, x, 1) != expectedIndirect ||
                NativeRejectedIndirect((nint)selectedPointer, x, 1) != expectedIndirect ||
                NativeCallbackPointer((nint)unmanagedPointer, x) != unchecked(x + 9))
                return 7;
        }
        if (NativeVoid() != 37)
            return 17;
        return TestGcTransitions();
    }

    private static int TestGcTransitions()
    {
        using var started = new ManualResetEventSlim();
        bool done = false;
        var collector = new Thread(() =>
        {
            started.Set();
            while (!Volatile.Read(ref done))
                GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        });
        byte[] root = new byte[1024];
        root[0] = 42;
        collector.Start();
        started.Wait();
        try
        {
            const int count = 50000;
            if (NativeTransitionStress(count) != count * (count + 1L) / 2)
                return 301;
            if (NativeForeignThread(123, 456) != 579 || root[0] != 42)
                return 302;
            GC.KeepAlive(root);
        }
        finally
        {
            Volatile.Write(ref done, true);
            collector.Join();
        }
        return 100;
    }

    private static int TestPreinitialization()
    {
        if (NativeClassConstructor(1) != 2 || TypeWithConstructor.Field != 23)
            return 5;

        long expected = 0;
        int narrow = 0;
        for (int i = 1; i < 12; i++)
        {
            narrow = unchecked((narrow + i) * 3 - i);
            expected += (narrow < 0 ? -1L : narrow) / 3L;
            expected = (expected << 1) | (uint)(narrow & 255);
        }
        return PreinitScalarFixture.Value == expected && PreinitScalarFixture.Comparison == 3 ? 100 : 17;
    }

    private static string FindBody(string module, string name)
    {
        Match match = Regex.Match(module, @"(?m)^define [^\r\n]*@""?" + Regex.Escape(name) + @"""?\([^\{]*\{([^}]+)\}");
        return match.Success ? match.Groups[1].Value : null;
    }

    private static void VerifyLlvmLto()
    {
        string managed = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LlvmLtoTests.Managed.ll"));
        string optimized = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "LlvmLtoTests.Optimized.ll"));
        string[] exports = [ "Add", "Subtract", "Wide", "Shift", "Convert", "Compare", "Locals", "Branch", "Float",
            "Phi", "Switch", "Narrow", "SmallArguments", "Enum", "NativeInt", "FloatArithmetic", "DoubleArithmetic", "FloatCompare",
            "UIntToDouble", "DoubleToInt", "DoubleToUInt", "DoubleToLong", "DoubleToULong", "DoubleToByte", "DoubleToSByte",
            "LocalAddress", "LocalBitcast", "Compose", "PointerGetter", "PointerSelect", "KnownCalli", "SafeDivide", "SafeRemainder",
            "Void", "Math", "MathF", "Callback", "Quoted%arg0", "QuotedPointerGetter" ];

        foreach (string name in exports)
        {
            string body = FindBody(managed, "Managed" + name) ?? throw new Exception("Missing LLVM export: Managed" + name);
            if (!body.Contains("@\"RhpReversePInvoke\"(") ||
                Regex.Matches(body, @"\bret\b").Count != Regex.Matches(body, "@\"RhpReversePInvokeReturn\"\\(").Count)
                throw new Exception("An unmanaged export lost its entry or exit transition: " + name);
            if (Regex.IsMatch(body, @"\b(call|invoke)\b(?![^\r\n]*@(?:llvm\.|""?(?:RhpReversePInvoke|__llvm_lto_inline_)))"))
                throw new Exception("A native frame could call the original managed body: " + name);
        }

        MatchCollection inlineBodies = Regex.Matches(managed, @"(?m)^define [^\r\n]*@""?__llvm_lto_inline_[^\r\n]*");
        if (inlineBodies.Count == 0)
            throw new Exception("Missing inline-only managed callees.");
        foreach (Match body in inlineBodies)
            if (!body.Value.Contains("available_externally") || !body.Value.Contains(" alwaysinline "))
                throw new Exception("A native callee could acquire an out-of-line implementation.");
        if (Regex.IsMatch(optimized, @"\b(call|invoke)\b[^\r\n]*@""?__llvm_lto_inline_"))
            throw new Exception("A required managed inline survived LLVM optimization.");

        if (Regex.IsMatch(managed, @"(?m)^define [^\r\n]*(Rejected|TypeWithConstructor)"))
            throw new Exception("An unsupported method was exported to LLVM.");

        // Inspect the linker's actual optimized IR instead of target-specific opcodes.
        foreach (string name in new[] { "NativeFold", "NativeKnownPointer", "NativePhi", "NativeCompose", "NativeKnownCalli", "NativeVoid", "NativeCallback", "NativeTransitionStress", "NativeThreadEntry" })
        {
            string body = FindBody(optimized, name) ?? throw new Exception("Missing native LTO body: " + name);
            // Scalar code is inlined, but required runtime transitions remain calls.
            if (Regex.IsMatch(body, @"\b(call|invoke)\b(?![^\r\n]*@(?:llvm\.|RhpReversePInvoke))"))
                throw new Exception("LLVM did not inline the managed body into " + name);
            if (!body.Contains("@RhpReversePInvoke(") || !body.Contains("@RhpReversePInvokeReturn("))
                throw new Exception("LLVM removed an unmanaged-entry transition in " + name);
        }
        foreach (string name in new[] { "NativeNoInline", "NativeCallbackPointer", "NativeRuntimePointer", "NativeRejectedIndirect", "NativeClassConstructor" })
        {
            string body = FindBody(optimized, name) ?? throw new Exception("Missing native LTO body: " + name);
            if (!Regex.IsMatch(body, @"\bcall\b"))
                throw new Exception("LLVM removed a required fallback call in " + name);
        }
        string indirect = FindBody(optimized, "NativeRuntimePointer");
        if (!Regex.IsMatch(indirect, @"\bcall\b[^@\r\n]*%[^\r\n]*\("))
            throw new Exception("A runtime function pointer did not remain indirect.");
        foreach (string name in new[] { "NativeThroughManagedPointer", "NativeQuotedPointer" })
        {
            string resolved = FindBody(optimized, name) ?? throw new Exception("Missing managed pointer getter test: " + name);
            if (Regex.IsMatch(resolved, @"@Managed(?:Quoted)?PointerGetter\(") || Regex.IsMatch(resolved, @"\bcall\b[^@\r\n]*%[^\r\n]*\("))
                throw new Exception("LLVM did not resolve the managed function pointer getter: " + name);
        }
    }
}
