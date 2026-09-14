// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class AddressThroughFields
{
    [StructLayout(LayoutKind.Explicit)]
    private struct Color
    {
        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;
        [FieldOffset(3)] public byte A;
        [FieldOffset(0)] public int Rgba;

        [UnscopedRef] public View<byte> Raw => new(ref this);
        [UnscopedRef] public View<short> SRaw => new(ref this);
    }

    private ref struct View<T> where T : unmanaged
    {
        public ref Color Target;

        public View(ref Color target) => Target = ref target;

        private static ref T Throw() => throw new IndexOutOfRangeException();

        public unsafe ref T this[uint index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => ref sizeof(T) * index >= sizeof(Color) ? ref Throw() :
                ref Unsafe.Add(ref Unsafe.As<Color, T>(ref Target), index);
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(2, Original());
        Assert.Equal(21, Overwrite(false));
        Assert.Equal(21, Overwrite(true));
        Assert.Equal(21, Escape());
        Assert.Equal(9, Observe());
        Assert.Equal(21, Loop(4));
        Assert.Equal(14, LoopCopy(4));
        Assert.Equal(21, MultipleFields());
        Assert.Equal(21, ReturnBuffer());
        Assert.Equal(21, Handler());
        Assert.Throws<NullReferenceException>(() => Clear());
        Assert.Throws<IndexOutOfRangeException>(() => ReadShort(2));
        Assert.Throws<IndexOutOfRangeException>(() => ReadShort(0x80000000));
        Assert.Throws<IndexOutOfRangeException>(() => ReadShort(uint.MaxValue));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static short ReadShort(uint index)
    {
        Color color = default;
        return color.SRaw[index];
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Original()
    {
        // X64-NOT: ptr [
        // X64-FULL-LINE: mov eax, 2
        // X64-NOT: ptr [
        // X64: ret
        var color = new Color { R = 1, G = 2, B = 3, A = 4 };
        color.Raw[2] = 3;
        color.SRaw[1] = 4;
        return color.G;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Overwrite(bool field)
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var view = first.Raw;
        view[0] = 3;
        if (field)
            view.Target = ref second;
        else
            view = second.Raw;
        view[1] = 9;
        return first.G * 3 + second.G * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Redirect(ref View<byte> view, [UnscopedRef] ref Color target) => view = new(ref target);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Escape()
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var view = first.Raw;
        view[0] = 3;
        Redirect(ref view, ref second);
        view[1] = 9;
        return first.G * 3 + second.G * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Read(View<byte> view) => view[1];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe()
    {
        Color color = new() { G = 2 };
        var view = color.Raw;
        view[1] = 9;
        return Read(view);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Loop(int count)
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var view = first.Raw;
        for (int i = 0; i < count; i++)
        {
            view[0] = (byte)i;
            Redirect(ref view, ref second);
        }
        view[1] = 9;
        return first.G * 3 + second.G * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Handler()
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var view = first.Raw;
        try
        {
            view[0] = 3;
            Redirect(ref view, ref second);
            throw new Exception();
        }
        catch (Exception)
        {
            view[1] = 9;
        }
        return first.G * 3 + second.G * 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int LoopCopy(int count)
    {
        Color first = new(), second = new();
        var view = first.Raw;
        for (int i = 1; i <= count; i++)
        {
            view[1] = (byte)i;
            view = second.Raw;
        }
        return first.G * 10 + second.G;
    }

    private ref struct Pair
    {
        public ref Color Target;
        public int Tag;
        public Pair(ref Color color, int tag) { Target = ref color; Tag = tag; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int MultipleFields()
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var pair = new Pair(ref first, 3);
        pair.Target.R = 3;
        pair = new Pair(ref second, 2);
        pair.Target.G = 9;
        return first.G * 3 + second.G * pair.Tag;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Pair CreatePair(ref Color color) => new(ref color, 2);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnBuffer()
    {
        Color first = new() { G = 1 }, second = new() { G = 2 };
        var pair = new Pair(ref first, 3);
        pair.Target.R = 3;
        pair = CreatePair(ref second);
        pair.Target.G = 9;
        return first.G * 3 + second.G * pair.Tag;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Clear()
    {
        Color color = new() { G = 2 };
        var view = color.Raw;
        view[0] = 3;
        view = default;
        return view[1];
    }
}
