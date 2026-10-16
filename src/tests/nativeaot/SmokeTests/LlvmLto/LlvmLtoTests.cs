// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

// This test deliberately uses the runtime helper ABI. The native helpers are
// bounded, non-allocating scalar functions called in cooperative mode. These
// attributes do not describe a supported public interop surface.
namespace System.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeExportAttribute : Attribute
    {
        public RuntimeExportAttribute(string name) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class RuntimeImportAttribute : Attribute
    {
        public RuntimeImportAttribute(string name) { }
    }
}

internal enum ScalarEnum : short { MinusOne = -1, Zero = 0, One = 1 }
internal enum MathOperation { Abs, Sqrt, Floor, Ceiling, Truncate, CopySign, Identity }

internal static unsafe class ManagedExports
{
    // Integer operations.

    [RuntimeExport("ManagedAdd")]
    public static int Add(int x, int y) => unchecked(x + y);

    [RuntimeExport("ManagedSubtract")]
    public static int Subtract(int x, int y) => unchecked(x - y);

    [RuntimeExport("ManagedWide")]
    public static long Wide(long x, long y) => unchecked((x - y) ^ (x * y));

    [RuntimeExport("ManagedShift")]
    public static long Shift(long x, int count) => (x << count) ^ (x >> count) ^ (long)((ulong)x >> count);

    [RuntimeExport("ManagedConvert")]
    public static long Convert(int x) => (long)x + (long)(uint)x;

    [RuntimeExport("ManagedCompare")]
    public static int Compare(ulong x, ulong y) => x > y ? 1 : 0;

    [RuntimeExport("ManagedLocals")]
    public static int Locals(int x, int y)
    {
        int saved = unchecked(x * y);
        x = ~x;
        return unchecked(saved + x - y);
    }

    [RuntimeExport("ManagedBranch")]
    public static int Branch(int x) => x < 0 ? -x : x;

    [RuntimeExport("ManagedPhi")]
    public static int Phi(int x, int y) => unchecked((x > y ? x : y) + (x < 0 ? 7 : 11));

    [RuntimeExport("ManagedSwitch")]
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

    [RuntimeExport("ManagedNarrow")]
    public static byte Narrow(int x) => unchecked((byte)(x + 129));

    [RuntimeExport("ManagedSmallArguments")]
    public static short SmallArguments(sbyte x, ushort y, char z, bool flag) => unchecked((short)(flag ? x + y + z : x - y - z));

    [RuntimeExport("ManagedEnum")]
    public static ScalarEnum Enum(ScalarEnum x) => unchecked((ScalarEnum)((short)x + 1));

    [RuntimeExport("ManagedNativeInt")]
    public static nint NativeInt(nint x, int count) => ((x + 5) >> count) ^ (nint)(uint)x;

    [RuntimeExport("ManagedSafeDivide")]
    public static int SafeDivide(int x) => x / 3;

    [RuntimeExport("ManagedSafeRemainder")]
    public static ulong SafeRemainder(ulong x) => x % 7;

    [RuntimeExport("ManagedLocalAddress")]
    public static int LocalAddress(int x, int y)
    {
        int value = x;
        ref int reference = ref value;
        reference = unchecked(reference + y);
        return value ^ x;
    }

    // Floating-point operations.

    [RuntimeExport("ManagedFloat")]
    public static double Floating(double x) => x + 1;

    [RuntimeExport("ManagedFloatArithmetic")]
    public static float FloatArithmetic(float x, float y) => (x + y) - x;

    [RuntimeExport("ManagedDoubleArithmetic")]
    public static double DoubleArithmetic(double x, double y) => (x - y) * 2.0 / y;

    [RuntimeExport("ManagedFloatCompare")]
    public static int FloatCompare(double x, double y) => x < y ? 1 : x > y ? 2 : x == y ? 3 : 4;

    [RuntimeExport("ManagedUIntToDouble")]
    public static double UIntToDouble(ulong x) => x;

    [RuntimeExport("ManagedDoubleToInt")]
    public static int DoubleToInt(double x) => (int)x;

    [RuntimeExport("ManagedDoubleToUInt")]
    public static uint DoubleToUInt(double x) => (uint)x;

    [RuntimeExport("ManagedDoubleToLong")]
    public static long DoubleToLong(double x) => (long)x;

    [RuntimeExport("ManagedDoubleToULong")]
    public static ulong DoubleToULong(double x) => (ulong)x;

    [RuntimeExport("ManagedDoubleToByte")]
    public static byte DoubleToByte(double x) => (byte)x;

    [RuntimeExport("ManagedDoubleToSByte")]
    public static sbyte DoubleToSByte(double x) => (sbyte)x;

    [RuntimeExport("ManagedLocalBitcast")]
    public static int LocalBitcast(float x) => *(int*)&x;

    [RuntimeExport("ManagedMath")]
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

    [RuntimeExport("ManagedMathF")]
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

    [RuntimeExport("ManagedCompose")]
    public static int Compose(int x) => ManagedExports.Add(x, 2) ^ ManagedExports.Subtract(x, 3);

    [RuntimeExport("ManagedPointerGetter")]
    public static delegate*<int, int, int> PointerGetter() => &ManagedExports.Add;

    [RuntimeExport("ManagedPointerSelect")]
    public static delegate*<int, int, int> PointerSelect(delegate*<int, int, int> left, delegate*<int, int, int> right, int choose) => choose == 0 ? left : right;

    [RuntimeExport("ManagedKnownCalli")]
    public static int KnownCalli(int x)
    {
        delegate*<int, int, int> callback = &ManagedExports.Add;
        return callback(x, 1);
    }

    [RuntimeExport("ManagedVoid")]
    public static void Empty() { }
}

internal static unsafe class RejectedExports
{
    public static int Field;

    [RuntimeExport("RejectedNoInline"), MethodImpl(MethodImplOptions.NoInlining)]
    public static int NoInline(int x) => unchecked(x + 7);

    [RuntimeExport("RejectedNoOptimization"), MethodImpl(MethodImplOptions.NoOptimization)]
    public static int NoOptimization(int x) => unchecked(x + 7);

    [RuntimeExport("RejectedCall")]
    public static int Call(int x) => NoInline(x);

    [RuntimeExport("RejectedField")]
    public static int ReadField() => Field;

    [RuntimeExport("RejectedChecked")]
    public static int Checked(int x) => checked(x + 1);

    [RuntimeExport("RejectedDivision")]
    public static int Divide(int x, int y) => x / y;

    [RuntimeExport("RejectedReference")]
    public static int Reference(object x) => x == null ? 0 : 1;

    [UnmanagedCallersOnly(EntryPoint = "RejectedUnmanagedEntry")]
    public static int UnmanagedEntry(int x) => unchecked(x + 9);

    [RuntimeExport("RejectedFunctionPointerCall")]
    public static unsafe int Indirect(delegate*<int, int, int> callback, int x, int y) => callback(x, y);

    [RuntimeExport("RejectedPointerRead")]
    public static int PointerRead(int* pointer) => *pointer;

    [RuntimeExport("RejectedDivideMinusOne")]
    public static int DivideMinusOne(int x) => x / -1;

    [RuntimeExport("RejectedLoop")]
    public static int Loop(int count) { int sum = 0; for (int i = 0; i < count; i++) sum += i; return sum; }

    [RuntimeExport("RejectedRecursive")]
    public static int Recursive(int x) => x == 0 ? 0 : Recursive(x - 1);

    [RuntimeExport("RejectedEH")]
    public static int ExceptionHandling(int x) { try { return x; } finally { RejectedExports.Field = x; } }
}

internal static class TypeWithConstructor
{
    public static int Field;
    static TypeWithConstructor() { Field = 23; }

    [RuntimeExport("RejectedClassConstructor")]
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
    [RuntimeImport("NativeWide"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern long NativeWide(long x, long y);
    [RuntimeImport("NativeShift"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern long NativeShift(long x, int count);
    [RuntimeImport("NativeConvert"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern long NativeConvert(int x);
    [RuntimeImport("NativeCompare"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeCompare(ulong x, ulong y);
    [RuntimeImport("NativeLocals"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeLocals(int x, int y);
    [RuntimeImport("NativePhi"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativePhi(int x, int y);
    [RuntimeImport("NativeSwitch"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeSwitch(int x);
    [RuntimeImport("NativeNarrow"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern byte NativeNarrow(int x);
    [RuntimeImport("NativeSmallArguments"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern short NativeSmallArguments(sbyte x, ushort y, char z, bool flag);
    [RuntimeImport("NativeEnum"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern ScalarEnum NativeEnum(ScalarEnum x);
    [RuntimeImport("NativeNativeInt"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern nint NativeNativeInt(nint x, int count);
    [RuntimeImport("NativeSafeDivide"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeSafeDivide(int x);
    [RuntimeImport("NativeSafeRemainder"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern ulong NativeSafeRemainder(ulong x);
    [RuntimeImport("NativeLocalAddress"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeLocalAddress(int x, int y);

    // Floating-point operations.
    [RuntimeImport("NativeFloatArithmetic"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern float NativeFloatArithmetic(float x, float y);
    [RuntimeImport("NativeDoubleArithmetic"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern double NativeDoubleArithmetic(double x, double y);
    [RuntimeImport("NativeFloatCompare"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeFloatCompare(double x, double y);
    [RuntimeImport("NativeUIntToDouble"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern double NativeUIntToDouble(ulong x);
    [RuntimeImport("NativeDoubleToInt"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeDoubleToInt(double x);
    [RuntimeImport("NativeDoubleToUInt"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern uint NativeDoubleToUInt(double x);
    [RuntimeImport("NativeDoubleToLong"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern long NativeDoubleToLong(double x);
    [RuntimeImport("NativeDoubleToULong"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern ulong NativeDoubleToULong(double x);
    [RuntimeImport("NativeDoubleToByte"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern byte NativeDoubleToByte(double x);
    [RuntimeImport("NativeDoubleToSByte"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern sbyte NativeDoubleToSByte(double x);
    [RuntimeImport("NativeLocalBitcast"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeLocalBitcast(float x);
    [RuntimeImport("NativeMath"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern double NativeMath(double x, double y, MathOperation operation);
    [RuntimeImport("NativeMathF"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern float NativeMathF(float x, float y, MathOperation operation);

    // Calls and function pointers.
    [RuntimeImport("NativeFold"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeFold(int x);
    [RuntimeImport("NativeCompose"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeCompose(int x);
    [RuntimeImport("NativeVoid"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeVoid();
    [RuntimeImport("NativeNoInline"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeNoInline(int x);
    [RuntimeImport("NativeKnownPointer"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeKnownPointer(int x);
    [RuntimeImport("NativeRuntimePointer"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeRuntimePointer(nint callback, int x, int y);
    [RuntimeImport("NativeGetPointer"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern nint NativeGetPointer();
    [RuntimeImport("NativeThroughManagedPointer"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeThroughManagedPointer(int x);
    [RuntimeImport("NativePointerSelect"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativePointerSelect(int x);
    [RuntimeImport("NativeKnownCalli"), MethodImpl(MethodImplOptions.InternalCall)]
    private static extern int NativeKnownCalli(int x);

    // Unmanaged callbacks retain both forward and reverse transitions.
    [DllImport("llvmltonative", EntryPoint = "NativeCallback")]
    private static extern int NativeCallback(int x);
    [DllImport("llvmltonative", EntryPoint = "NativeCallbackPointer")]
    private static extern int NativeCallbackPointer(nint callback, int x);

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
            if (NativeSwitch(x) != ManagedExports.Switch(x)) return 101;
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
                    NativeSmallArguments((sbyte)x, (ushort)y, (char)x, (y & 1) == 0) !=
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
        delegate*<int, int, int> addPointer = &ManagedExports.Add;
        delegate*<int, int, int> subtractPointer = &ManagedExports.Subtract;
        delegate* unmanaged<int, int> unmanagedPointer = &RejectedExports.UnmanagedEntry;
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
            if (NativePointerSelect(x) != ((x & 1) == 0 ? unchecked(x + 1) : unchecked(x - 1))) return 107;

            delegate*<int, int, int> selectedPointer = (x & 1) == 0 ? addPointer : subtractPointer;
            int expectedIndirect = (x & 1) == 0 ? unchecked(x + 1) : unchecked(x - 1);
            if (NativeKnownPointer(x) != (unchecked(x + 1) ^ 0x1234) ||
                NativeRuntimePointer((nint)selectedPointer, x, 1) != expectedIndirect ||
                RejectedExports.Indirect(selectedPointer, x, 1) != expectedIndirect ||
                NativeCallbackPointer((nint)unmanagedPointer, x) != unchecked(x + 9))
                return 7;
        }
        return NativeVoid() == 37 ? 100 : 17;
    }

    private static int TestPreinitialization()
    {
        if (TypeWithConstructor.Leaf(1) != 2 || TypeWithConstructor.Field != 23)
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
            "Void", "Math", "MathF" ];

        foreach (string name in exports)
            if (FindBody(managed, "Managed" + name) == null)
                throw new Exception("Missing LLVM export: Managed" + name);

        if (Regex.IsMatch(managed, @"(?m)^define [^\r\n]*(Rejected|TypeWithConstructor)"))
            throw new Exception("An unsupported method was exported to LLVM.");

        // Inspect the linker's actual optimized IR instead of target-specific opcodes.
        foreach (string name in new[] { "NativeFold", "NativeKnownPointer", "NativePhi", "NativeCompose", "NativeKnownCalli", "NativeVoid" })
        {
            string body = FindBody(optimized, name) ?? throw new Exception("Missing native LTO body: " + name);
            // LLVM may represent instructions such as integer max as intrinsic calls.
            if (Regex.IsMatch(body, @"\b(call|invoke)\b(?![^\r\n]*@llvm\.)"))
                throw new Exception("LLVM did not inline the managed body into " + name);
        }
        foreach (string name in new[] { "NativeNoInline", "NativeCallback", "NativeCallbackPointer", "NativeRuntimePointer" })
        {
            string body = FindBody(optimized, name) ?? throw new Exception("Missing native LTO body: " + name);
            if (!Regex.IsMatch(body, @"\bcall\b"))
                throw new Exception("LLVM removed a required fallback call in " + name);
        }
        string indirect = FindBody(optimized, "NativeRuntimePointer");
        if (!Regex.IsMatch(indirect, @"\bcall\b[^\r\n]*%[^\r\n]*\("))
            throw new Exception("A runtime function pointer did not remain indirect.");
        string resolved = FindBody(optimized, "NativeThroughManagedPointer") ?? throw new Exception("Missing managed pointer getter test.");
        if (resolved.Contains("@ManagedPointerGetter(") || Regex.IsMatch(resolved, @"\bcall\b[^\r\n]*%[^\r\n]*\("))
            throw new Exception("LLVM did not resolve the managed function pointer getter.");
    }
}
