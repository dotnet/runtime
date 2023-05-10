// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

// we will be doing "sizeof" with arrays containing managed references.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

[InlineArray(LengthConst)]
struct MyArray<T> : IEnumerable<T>
{
    private const int LengthConst = 42;
    private T _element;

    public int Length => LengthConst;

    [UnscopedRef]
    public ref T this[int i]
    {
        get
        {
            if ((uint)i >= (uint)Length)
                throw new IndexOutOfRangeException(nameof(i));

            return ref Unsafe.Add(ref _element, i);
        }
    }

    [UnscopedRef]
    public Span<T> AsSpan() => MemoryMarshal.CreateSpan<T>(ref _element, Length);

    IEnumerator IEnumerable.GetEnumerator() => (IEnumerator<T>)this.GetEnumerator();

    public IEnumerator<T> GetEnumerator()
    {
        for (int i =0; i < Length; i++)
        {
            yield return this[i];
        }
    }
}

unsafe class Validate
{
    // ====================== SizeOf ==============================================================
    [InlineArray(42)]
    struct FourtyTwoBytes
    {
        byte b;
    }

    [Fact]
    public static void Sizeof()
    {
        Console.WriteLine($"{nameof(Sizeof)}...");
        Assert.Equal(42, sizeof(FourtyTwoBytes));
        Assert.Equal(84, sizeof(MyArray<char>));
    }

    // ====================== OneElement ==========================================================
    [InlineArray(1)]
    struct OneObj
    {
        public object obj;
    }

    [Fact]
    public static void OneElement()
    {
        Console.WriteLine($"{nameof(OneElement)}...");
        Assert.Equal(sizeof(nint), sizeof(OneObj));
    }

    // ====================== UseOnStack ==========================================================
    class One { }
    class Two { }
    class Three { }
    class Four { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe Arr1 Initialize(Arr1 s)
    {
        s[0].o = new One();
        s[1].o = new Two();
        s[2].o = new Three();
        s[3].o = new Four();
        return s;
    }

    struct E
    {
        public int x;
        public int y;
        public object o;
    }

    [InlineArray(Length)]
    struct Arr1
    {
        public const int Length = 42;
        public E e;

        [UnscopedRef]
        public ref E this[int i] => ref Unsafe.Add(ref e, i);
    }

    static object s;
    private static unsafe void MakeGarbage()
    {
        // make garbage
        for (int i = 0; i < 10000; i++)
        {
            s = new int[i];
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PassByValueDoGcAndValidate(Arr1 s1)
    {
        MakeGarbage();

        GC.Collect(2, GCCollectionMode.Forced, true, true);
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        Assert.Equal("One", s1[0].o.GetType().Name);
        Assert.Equal("Two", s1[1].o.GetType().Name);
        Assert.Equal("Three", s1[2].o.GetType().Name);
        Assert.Equal("Four", s1[3].o.GetType().Name);
    }

    [Fact]
    public static void UseOnStack()
    {
        Console.WriteLine($"{nameof(UseOnStack)}...");

        Arr1 s = default;
        MakeGarbage();

        s = Initialize(s);

        // use as a byval argument
        PassByValueDoGcAndValidate(s);

        // refs must be separate and alive
        Assert.Equal("One", s[0].o.GetType().Name);
        Assert.Equal("Two", s[1].o.GetType().Name);
        Assert.Equal("Three", s[2].o.GetType().Name);
        Assert.Equal("Four", s[3].o.GetType().Name);

        // should copy by value
        Arr1 s1 = s;
        Assert.Equal("One", s1[0].o.GetType().Name);
        Assert.Equal("Two", s1[1].o.GetType().Name);
        Assert.Equal("Three", s1[2].o.GetType().Name);
        Assert.Equal("Four", s1[3].o.GetType().Name);
    }

    // ====================== MixObjectsAndValuetypes =============================================
    [InlineArray(Length)]
    struct ObjShortArr
    {
        public const int Length = 100;
        public (object, short) element;

        [UnscopedRef]
        public ref (object o, short s) this[int i] => ref Unsafe.Add(ref element, i);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjShortArr CreateArray(int recCount) {
            if (recCount > 0) {
                return CreateArray(recCount-1);
            } else {
                var arr = new ObjShortArr();
                for (short i = 0; i < ObjShortArr.Length; i++)
                {
                    arr[i].o = i;
                    arr[i].s = (short)(i + 1);
                }
                return arr;
            }
        }
    }

    [Fact]
    public static void MixObjectsAndValuetypes()
    {
        Console.WriteLine($"{nameof(MixObjectsAndValuetypes)}...");
        Assert.Equal(ObjShortArr.Length * sizeof(nint) * 2, sizeof(ObjShortArr));

        var arr = new ObjShortArr();
        for (short i = 0; i < ObjShortArr.Length; i++)
        {
            arr[i].o = i;
            arr[i].s = (short)(i + 1);
        }

        GC.Collect(2, GCCollectionMode.Forced, true, true);

        for (short i = 0; i < ObjShortArr.Length; i++)
        {
            Assert.Equal(i, arr[i].o);
            Assert.Equal(i + 1, arr[i].s);
        }
    }

    // ====================== RefLikeOuter ========================================================
    [InlineArray(Length)]
    ref struct ObjShortArrRef
    {
        public const int Length = 100;
        public (object, short) element;

        [UnscopedRef]
        public ref (object o, short s) this[int i] => ref Unsafe.Add(ref element, i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestRefLikeOuterMethodArg(ObjShortArrRef arr)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        for (short i = 0; i < ObjShortArrRef.Length; i++)
        {
            Assert.Equal(i * 2, arr[i].o);
            Assert.Equal(i * 2 + 1, arr[i].s);
        }
    }

    [Fact]
    public static void RefLikeOuter()
    {
        Console.WriteLine($"{nameof(RefLikeOuter)}...");

        var arr = new ObjShortArrRef();
        for (short i = 0; i < ObjShortArrRef.Length; i++)
        {
            arr[i].o = i;
            arr[i].s = (short)(i + 1);
        }

        GC.Collect(2, GCCollectionMode.Forced, true, true);

        for (short i = 0; i < ObjShortArrRef.Length; i++)
        {
            Assert.Equal(i, arr[i].o);
            Assert.Equal(i + 1, arr[i].s);
        }

        for (short i = 0; i < ObjShortArrRef.Length; i++)
        {
            arr[i].o = i * 2;
            arr[i].s = (short)(i * 2 + 1);
        }

        TestRefLikeOuterMethodArg(arr);
    }

    // ====================== RefLikeInner ========================================================
    [InlineArray(LengthConst)]
    ref struct SpanArr
    {
        private const int LengthConst = 100;
        public Span<object> element;

        public Span<object>* this[int i]
        {
            get
            {
                fixed (Span<object>* p = &element)
                {
                    return p + i;
                }
            }
        }

        public int Length => LengthConst;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void TestRefLikeInnerMethodArg(SpanArr arr)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        for (int i = 1; i < arr.Length; i++)
        {
            Assert.Equal(i, arr[i]->Length);
            Assert.Equal(i, (*arr[i])[0]);
        }
    }

    [Fact]
    public static void RefLikeInner()
    {
        Console.WriteLine($"{nameof(RefLikeInner)}...");

        SpanArr arr = default;
        for (int i = 1; i < arr.Length; i++)
        {
            var objArr = new object[i];
            objArr[0] = i;
            *arr[i] = objArr;
        }

        TestRefLikeInnerMethodArg(arr);
    }

    // ====================== Nested ==============================================================

    struct IntObj
    {
        public int i;
        public object o;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void NestedMethodArg(ref MyArray<MyArray<IntObj>> nestedArray)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        for (int i = 0; i < nestedArray.Length; i++)
        {
            for (int j = 0; j < nestedArray[i].Length; j++)
            {
                Assert.Equal(i + j, nestedArray[i][j].o);
                Assert.Equal(i * j, nestedArray[i][j].i);
            }
        }
    }

    [Fact]
    public static void Nested()
    {
        Console.WriteLine($"{nameof(Nested)}...");

        MyArray<MyArray<IntObj>> nestedArray = default;

        for(int i = 0; i < nestedArray.Length; i++)
        {
            for (int j = 0; j < nestedArray[i].Length; j++)
            {
                nestedArray[i][j].o = i + j;
                nestedArray[i][j].i = i * j;
            }
        }
    }

    // ====================== Boxed ===============================================================

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void BoxedMethodArg(IEnumerable<object> arr)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);

        int i = 0;
        foreach(object obj in arr)
        {
            Assert.Equal(i++, obj);
        }
    }

    [Fact]
    public static void Boxed()
    {
        Console.WriteLine($"{nameof(Boxed)}...");

        MyArray<object> arr = default;
        for (int i = 0; i < arr.Length; i++)
        {
            arr[i] = i;
        }

        BoxedMethodArg(arr);
    }

    // ====================== GCDescOpt ==========================================================

    [Fact]
    [SkipOnMono("CoreCLR and NativeAOT-specific implementation details.")]
    public static void GCDescOpt()
    {
        Console.WriteLine($"{nameof(GCDescOpt)}...");

        MyArray<object>[] arr = new MyArray<object>[5];

        fixed (void* arrPtr = arr)
        {
            nint* mtPtr = (nint*)arrPtr - 2;
            nint* gcSeriesPtr = (nint*)*mtPtr - 1;

            // optimized gc info should have exactly 1 gc series.
            Assert.Equal(1, *gcSeriesPtr);
        }
    }

     // ====================== MonoGCDesc ==========================================================

    class Holder {
        public ObjShortArr arr;
    }

    static Holder CreateArray() {
        var arr = ObjShortArr.CreateArray(100);
        var holder = new Holder();
        holder.arr = arr;
        return holder;
    }

    [Fact]
    public static void MonoGCDescOpt()
    {
        Console.WriteLine($"{nameof(MonoGCDescOpt)}...");

        var holder = CreateArray();

        GC.Collect(2, GCCollectionMode.Forced, true, true);

        MakeGarbage();

        for (short i = 0; i < ObjShortArr.Length; i++)
        {
            Assert.Equal(i, holder.arr[i].o);
            Assert.Equal(i + 1, holder.arr[i].s);
        }
    }
}
