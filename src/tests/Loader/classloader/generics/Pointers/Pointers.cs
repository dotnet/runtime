// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

unsafe class Pointers
{
    private struct Struct { public int Num; }

    private class Test<T> where T : unmanaged
    {
        public T Pointer(T* pointer) => *pointer;

        public T[] PointerArray(T*[] array)
        {
            T[] res = new T[array.Length];
            for (int i = 0; i < array.Length; i++)
                res[i] = *array[i];

            return res;
        }

        public void FunctionPointer(delegate*<T, void> func) => func(default);

        public void FunctionPointerArray(delegate*<T, void>[] array)
        {
            foreach (var func in array)
                func(default);
        }
    }

    private class TestTwoParams<T, U> where T : unmanaged where U : unmanaged
    {
        public (T, U) Pointer(T* pointer1, U* pointer2) => (*pointer1, *pointer2);

        public (T[], U[]) PointerArray(T*[] array1, U*[] array2)
        {
            T[] res1 = new T[array1.Length];
            for (int i = 0; i < array1.Length; i++)
                res1[i] = *array1[i];

            U[] res2 = new U[array2.Length];
            for (int i = 0; i < array2.Length; i++)
                res2[i] = *array2[i];

            return (res1, res2);
        }

        public void FunctionPointer(delegate*<T, void> func1, delegate*<U, void> func2)
        {
            func1(default);
            func2(default);
        }

        public void FunctionPointerArray(delegate*<T, void>[] array1, delegate*<U, void>[] array2)
        {
            foreach (var func in array1)
                func(default);

            foreach (var func in array2)
                func(default);
        }
    }

    [Fact]
    public static void Pointer()
    {
        Console.WriteLine($"Validating {nameof(Pointer)}...");
        PointerImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PointerImpl()
    {
        int i = 0;
        Struct s = default;
        {
            var t = new Test<int>();
            int res = t.Pointer(&i);
            Assert.Equal(i, res);
        }
        {
            var t = new Test<Struct>();
            Struct res = t.Pointer(&s);
            Assert.Equal(s, res);
        }
        {
            var t = new TestTwoParams<int, Struct>();
            (int res1, Struct res2) = t.Pointer(&i, &s);
            Assert.Equal(i, res1);
            Assert.Equal(s, res2);
        }
    }

    [Fact]
    public static void PointerArray()
    {
        Console.WriteLine($"Validating {nameof(PointerArray)}...");
        PointerArrayImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PointerArrayImpl()
    {
        int*[] intPtrArray = new int*[5];
        Span<int> intSpan = stackalloc int[intPtrArray.Length];
        int* intArray = (int*)Unsafe.AsPointer(ref intSpan.GetPinnableReference());
        for (int i = 0; i < intPtrArray.Length; i++)
        {
            intArray[i] = i;
            intPtrArray[i] = &intArray[i];
        }

        Struct*[] structPtrArray = new Struct*[5];
        Span<Struct> structSpan = stackalloc Struct[structPtrArray.Length];
        Struct* structArray = (Struct*)Unsafe.AsPointer(ref structSpan.GetPinnableReference());
        for (int i = 0; i < structPtrArray.Length; i++)
        {
            structArray[i] = new Struct() { Num = i };
            structPtrArray[i] = &structArray[i];
        }

        {
            var t = new Test<int>();
            int[] res = t.PointerArray(intPtrArray);
            Assert.True(intSpan.SequenceEqual(res));
        }
        {
            var t = new Test<Struct>();
            Struct[] res = t.PointerArray(structPtrArray);
            Assert.True(structSpan.SequenceEqual(res));
        }
        {
            var t = new TestTwoParams<int, Struct>();
            (int[] res1, Struct[] res2) = t.PointerArray(intPtrArray, structPtrArray);
            Assert.True(intSpan.SequenceEqual(res1));
            Assert.True(structSpan.SequenceEqual(res2));
        }
    }

    [Fact]
    public static void FunctionPointer()
    {
        Console.WriteLine($"Validating {nameof(FunctionPointer)}...");
        FunctionPointerImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FunctionPointerImpl()
    {
        s_takeIntCallCount = 0;
        s_takeStructCallCount = 0;
        {
            var t = new Test<int>();
            t.FunctionPointer(&TakeInt);
            Assert.Equal(1, s_takeIntCallCount);
        }
        {
            var t = new Test<Struct>();
            t.FunctionPointer(&TakeStruct);
            Assert.Equal(1, s_takeStructCallCount);
        }
        {
            var t = new TestTwoParams<int, Struct>();
            t.FunctionPointer(&TakeInt, &TakeStruct);
            Assert.Equal(2, s_takeIntCallCount);
            Assert.Equal(2, s_takeStructCallCount);
        }

    }

    [Fact]
    public static void FunctionPointerArray()
    {
        Console.WriteLine($"Validating {nameof(FunctionPointerArray)}...");
        FunctionPointerArrayImpl();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void FunctionPointerArrayImpl()
    {
        int length = 5;
        delegate*<int, void>[] intFuncArray = new delegate*<int, void>[length];
        delegate*<Struct, void>[] structFuncArray = new delegate*<Struct, void>[length];
        for (int i = 0; i < length; i++)
        {
            intFuncArray[i] = &TakeInt;
            structFuncArray[i] = &TakeStruct;
        }

        s_takeIntCallCount = 0;
        s_takeStructCallCount = 0;
        {
            var t = new Test<int>();
            t.FunctionPointerArray(intFuncArray);
            Assert.Equal(length, s_takeIntCallCount);
        }
        {
            var t = new Test<Struct>();
            t.FunctionPointerArray(structFuncArray);
            Assert.Equal(length, s_takeStructCallCount);
        }
        {
            var t = new TestTwoParams<int, Struct>();
            t.FunctionPointerArray(intFuncArray, structFuncArray);
            Assert.Equal(length * 2, s_takeIntCallCount);
            Assert.Equal(length * 2, s_takeStructCallCount);
        }
    }

    static int s_takeIntCallCount = 0;
    private static void TakeInt(int _) => s_takeIntCallCount++;

    static int s_takeStructCallCount = 0;
    private static void TakeStruct(Struct _) => s_takeStructCallCount++;
}
