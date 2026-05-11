// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Debuggee for cDAC dump tests - exercises local variable and argument
/// inspection via IXCLRDataValue across a variety of types and calling patterns.
/// Each method uses NoInlining and try/finally+GC.KeepAlive to ensure locals
/// are stored as real IL local variables with debug info.
///
/// Type coverage by frame (simple -> complex):
///
///   PrimitiveVars          - I4, R8, BOOLEAN, CHAR, U1, I2, I8, R4
///   NativeIntVars          - I (nint), U (nuint)
///   StructVars             - VALUETYPE (TinyStruct 1B, SmallStruct 8B, LargeStruct 32B)
///   ReferenceTypeVars      - STRING, CLASS, VALUETYPE(enum), OBJECT, SZARRAY
///   GenericInstAndByRefVars - GENERICINST+CLASS, GENERICINST+VALUETYPE, BYREF
///   InstanceMethodOnStruct - VALUETYPE as arg to static method
///   InstanceMethodVars    - instance 'this' (IS_REFERENCE) + I4
///   ClassGenericVars       - VAR (class-level generic parameter)
///   MethodGenericVars      - MVAR (method-level generic parameter)
///   SingleDimArrayVars     - SZARRAY (single-dimensional array)
///   MultiDimArrayVars      - ARRAY (multi-dimensional array)
///   PointerVars            - PTR (unmanaged pointer), FNPTR (function pointer)
/// </summary>
internal static class Program
{
    public struct TinyStruct { public byte Value; }
    public struct SmallStruct { public int X; public int Y; }
    public struct LargeStruct { public long A; public long B; public long C; public long D; }

    public class SimpleClass { public int Value; public string? Name; }

    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    private static void Main()
    {
        PrimitiveVars(42, 3.14, true, 'Z', (byte)0xFF, (short)-1, 123456789L, 2.5f);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PrimitiveVars(int intArg, double doubleArg, bool boolArg, char charArg,
        byte byteArg, short shortArg, long longArg, float floatArg)
    {
        int localInt = intArg + 1;
        double localDouble = doubleArg * 2;
        bool localBool = !boolArg;
        char localChar = (char)(charArg - 1);
        byte localByte = (byte)(byteArg - 1);
        short localShort = (short)(shortArg + 1);
        long localLong = longArg * 2;
        float localFloat = floatArg + 1.0f;
        try
        {
            NativeIntVars((nint)0x1234, (nuint)0x5678);
        }
        finally
        {
            GC.KeepAlive(localInt);
            GC.KeepAlive(localDouble);
            GC.KeepAlive(localBool);
            GC.KeepAlive(localChar);
            GC.KeepAlive(localByte);
            GC.KeepAlive(localShort);
            GC.KeepAlive(localLong);
            GC.KeepAlive(localFloat);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NativeIntVars(nint nintArg, nuint nuintArg)
    {
        nint localNint = nintArg;
        nuint localNuint = nuintArg;
        try
        {
            StructVars(
                new TinyStruct { Value = 42 },
                new SmallStruct { X = 10, Y = 20 },
                new LargeStruct { A = 1, B = 2, C = 3, D = 4 });
        }
        finally
        {
            GC.KeepAlive(localNint);
            GC.KeepAlive(localNuint);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StructVars(TinyStruct tinyArg, SmallStruct smallArg, LargeStruct largeArg)
    {
        TinyStruct localTiny = tinyArg;
        SmallStruct localSmall = smallArg;
        LargeStruct localLarge = largeArg;
        try
        {
            ReferenceTypeVars("test", new SimpleClass { Value = 99, Name = "hello" }, Color.Blue, 42, [1, 2, 3]);
        }
        finally
        {
            GC.KeepAlive(localTiny);
            GC.KeepAlive(localSmall);
            GC.KeepAlive(localLarge);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ReferenceTypeVars(string stringArg, SimpleClass classArg, Color enumArg, object boxedArg, int[] arrayArg)
    {
        string localString = stringArg + " world";
        SimpleClass localClass = classArg;
        Color localEnum = enumArg;
        object localBoxed = boxedArg;
        int[] localArray = arrayArg;
        try
        {
            int refTarget = 42;
            GenericInstAndByRefVars(new List<int> { 1, 2, 3 }, new KeyValuePair<int, string>(1, "one"), ref refTarget);
        }
        finally
        {
            GC.KeepAlive(localString);
            GC.KeepAlive(localClass);
            GC.KeepAlive(localEnum);
            GC.KeepAlive(localBoxed);
            GC.KeepAlive(localArray);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void GenericInstAndByRefVars(List<int> listArg, KeyValuePair<int, string> kvpArg, ref int refArg)
    {
        List<int> localList = listArg;
        KeyValuePair<int, string> localKvp = kvpArg;
        int localRefCopy = refArg;
        try
        {
            InstanceMethodOnStruct(new SmallStruct { X = 100, Y = 200 });
        }
        finally
        {
            GC.KeepAlive(localList);
            GC.KeepAlive(localKvp);
            GC.KeepAlive(localRefCopy);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void InstanceMethodOnStruct(SmallStruct s)
    {
        var wrapper = new InstanceWrapper(s.X);
        wrapper.InstanceMethodVars(s.Y);
    }

    public class InstanceWrapper
    {
        private readonly int _value;
        public InstanceWrapper(int value) => _value = value;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void InstanceMethodVars(int extra)
        {
            int localSum = _value + extra;
            try
            {
                var container = new GenericContainer<int>(localSum);
                container.ClassGenericVars(localSum);
            }
            finally
            {
                GC.KeepAlive(localSum);
            }
        }
    }

    public class GenericContainer<T>
    {
        private readonly T _item;
        public GenericContainer(T item) => _item = item;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ClassGenericVars(T value)
        {
            T localCopy = value;
            try
            {
                MethodGenericVars<T, string>(localCopy, _item?.ToString() ?? "generic");
            }
            finally
            {
                GC.KeepAlive(localCopy);
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MethodGenericVars<T1, T2>(T1 arg1, T2 arg2)
    {
        T1 local1 = arg1;
        T2 local2 = arg2;
        try
        {
            SingleDimArrayVars([10, 20, 30]);
        }
        finally
        {
            GC.KeepAlive(local1);
            GC.KeepAlive(local2);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SingleDimArrayVars(int[] arrayArg)
    {
        int[] localArray = arrayArg;
        try
        {
            MultiDimArrayVars(new int[2, 3] { { 1, 2, 3 }, { 4, 5, 6 } });
        }
        finally
        {
            GC.KeepAlive(localArray);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MultiDimArrayVars(int[,] multiDimArg)
    {
        int[,] localMultiDim = multiDimArg;
        try
        {
            unsafe
            {
                int stackValue = 999;
                PointerVars(&stackValue, &DoubleValue);
            }
        }
        finally
        {
            GC.KeepAlive(localMultiDim);
        }
    }

    private static int DoubleValue(int x) => x * 2;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe void PointerVars(int* ptrArg, delegate*<int, int> funcPtrArg)
    {
        int localDeref = *ptrArg;
        int localFuncResult = funcPtrArg(localDeref);
        try
        {
            Crash();
        }
        finally
        {
            GC.KeepAlive(localDeref);
            GC.KeepAlive(localFuncResult);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Crash()
    {
        Environment.FailFast("cDAC dump test: LocalVariables debuggee intentional crash");
    }
}
