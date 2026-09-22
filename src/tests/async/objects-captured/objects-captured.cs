// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Async2ObjectsWithYields
{
    [StructLayout(LayoutKind.Sequential)]
    private sealed class References
    {
        public object A;
        public object B;
        public object C;
        public object D;
        public int Marker;
    }

    [StructLayout(LayoutKind.Explicit)]
    private sealed class SparseReferences
    {
        [FieldOffset(0)] public object A;
        [FieldOffset(4096)] public object B;
        [FieldOffset(8192)] public object C;
        [FieldOffset(16384)] public object D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SparseReferences AllocateFarApart(object a, object b, object c, object d)
    {
        return new SparseReferences { A = a, B = b, C = c, D = d };
    }

    [Fact]
    public static void FieldsAcrossCards()
    {
        object a = new object();
        object b = new object();
        object c = new object();
        object d = new object();
        SparseReferences refs = AllocateFarApart(a, b, c, d);
        GC.Collect();
        Assert.Same(a, refs.A);
        Assert.Same(b, refs.B);
        Assert.Same(c, refs.C);
        Assert.Same(d, refs.D);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateReferences(object a, object b, object c, object d)
    {
        return new References { A = a, B = b, C = c, D = d };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateReversed(object a, object b, object c, object d)
    {
        return new References { D = d, C = c, B = b, A = a };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithGap(object a, object c)
    {
        return new References { A = a, C = c };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithThreeGappedFields(object a, object c, object d)
    {
        return new References { A = a, C = c, D = d };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithOverwrite(object a, object b, object c, object d)
    {
        References refs = new References();
        refs.A = c;
        refs.B = b;
        refs.A = a;
        refs.C = c;
        refs.D = d;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ObserveReferences(References refs, object a)
    {
        Assert.Same(a, refs.A);
        Assert.Null(refs.B);
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithCall(object a, object b, object c, object d)
    {
        References refs = new References();
        refs.A = a;
        ObserveReferences(refs, a);
        refs.B = b;
        refs.C = c;
        refs.D = d;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateInterleaved(object a, object b, object c, object d)
    {
        References refs = new References();
        refs.A = a;
        refs.Marker = 1;
        refs.B = b;
        refs.Marker = 2;
        refs.C = c;
        refs.D = d;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateFromFields(References source)
    {
        References refs = new References();
        refs.A = source.A;
        refs.B = source.B;
        refs.C = source.C;
        refs.D = source.D;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithDependentRead(object a, object b, object c, object d)
    {
        References refs = new References();
        refs.A = a;
        refs.B = refs.A;
        refs.C = c;
        refs.D = d;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateWithLateDependentRead(object a, object b, object c)
    {
        References refs = new References();
        refs.A = a;
        refs.B = b;
        refs.C = c;
        refs.D = refs.A;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ObservePopulatedReferences(References refs, object a, object b, object c)
    {
        Assert.Same(a, refs.A);
        Assert.Same(b, refs.B);
        Assert.Same(c, refs.C);
        Assert.Null(refs.D);
        GC.Collect();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateThenObserve(object a, object b, object c, object d)
    {
        References refs = new References();
        refs.A = a;
        refs.B = b;
        refs.C = c;
        ObservePopulatedReferences(refs, a, b, c);
        refs.D = d;
        return refs;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static References AllocateAcrossBlocks(object a, object b, object c, object d, bool useC)
    {
        References refs = new References();
        refs.A = a;
        refs.B = b;
        if (useC)
        {
            refs.C = c;
            refs.Marker = 1;
        }
        else
        {
            refs.C = d;
            refs.Marker = 2;
        }
        refs.D = d;
        return refs;
    }

    [Fact]
    public static void NullFieldSource()
    {
        Assert.Throws<NullReferenceException>(() => AllocateFromFields(null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    public static void NewlyAllocatedFields(int shape)
    {
        object a = new object();
        object b = new object();
        object c = new object();
        object d = new object();
        References refs = shape switch
        {
            0 => AllocateReferences(a, b, c, d),
            1 => AllocateReversed(a, b, c, d),
            2 => AllocateWithGap(a, c),
            3 => AllocateWithOverwrite(a, b, c, d),
            4 => AllocateWithCall(a, b, c, d),
            5 => AllocateInterleaved(a, b, c, d),
            6 => AllocateFromFields(new References { A = a, B = b, C = c, D = d }),
            7 => AllocateWithDependentRead(a, b, c, d),
            8 => AllocateWithLateDependentRead(a, b, c),
            9 => AllocateThenObserve(a, b, c, d),
            10 or 11 => AllocateAcrossBlocks(a, b, c, d, shape == 10),
            12 => AllocateWithThreeGappedFields(a, c, d),
            _ => throw new ArgumentOutOfRangeException(nameof(shape)),
        };
        GC.Collect();
        Assert.Same(a, refs.A);
        Assert.Same(shape is 2 or 12 ? null : shape == 7 ? a : b, refs.B);
        Assert.Same(shape == 11 ? d : c, refs.C);
        Assert.Same(shape == 2 ? null : shape == 8 ? a : d, refs.D);
        Assert.Equal(shape switch { 5 or 11 => 2, 10 => 1, _ => 0 }, refs.Marker);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task CaptureReferences(TaskCompletionSource[] gates)
    {
        for (int i = 0; i < gates.Length; i++)
        {
            object a = i * 8;
            object b = i * 8 + 1;
            object c = i * 8 + 2;
            object d = i * 8 + 3;
            object e = i * 8 + 4;
            object f = i * 8 + 5;
            object g = i * 8 + 6;
            object h = i * 8 + 7;

            await gates[i].Task;

            Assert.Equal(i * 8, (int)a);
            Assert.Equal(i * 8 + 1, (int)b);
            Assert.Equal(i * 8 + 2, (int)c);
            Assert.Equal(i * 8 + 3, (int)d);
            Assert.Equal(i * 8 + 4, (int)e);
            Assert.Equal(i * 8 + 5, (int)f);
            Assert.Equal(i * 8 + 6, (int)g);
            Assert.Equal(i * 8 + 7, (int)h);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task CaptureSparseReferences(TaskCompletionSource[] gates)
    {
        object a = null;
        object stable1 = new object();
        object b = null;
        object stable2 = new object();
        object c = null;
        for (int i = 0; i < gates.Length; i++)
        {
            a = i * 3;
            b = i * 3 + 1;
            c = i * 3 + 2;
            await gates[i].Task;
            Assert.Equal(i * 3, (int)a);
            Assert.Equal(i * 3 + 1, (int)b);
            Assert.Equal(i * 3 + 2, (int)c);
            GC.KeepAlive(stable1);
            GC.KeepAlive(stable2);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void ReusedContinuationFields(bool sparse)
    {
        TaskCompletionSource[] gates = new TaskCompletionSource[4];
        for (int i = 0; i < gates.Length; i++)
        {
            gates[i] = new TaskCompletionSource();
        }

        Task task = sparse ? CaptureSparseReferences(gates) : CaptureReferences(gates);
        // Promote the suspended continuation, then write young references into
        // it on subsequent suspensions and collect before reading them back.
        GC.Collect();
        for (int i = 0; i < gates.Length; i++)
        {
            Assert.False(task.IsCompleted);
            GC.Collect(0);
            gates[i].SetResult();
        }
        task.GetAwaiter().GetResult();
    }

    internal static async Task<int> A(object n)
    {
        // use string equality so that JIT would not think of hoisting "(int)n"
        // also to produce some amout of garbage
        if (n.ToString() != 0.ToString())
        {
            return await A((int)n - 1) + (int)n;
        }

        await Task.Yield();
        return 0;
    }

    [RuntimeAsyncMethodGeneration(false)]
    private static async Task<int> AsyncEntry()
    {
        object result = 0;
        for (int i = 0; i < 20; i++)
        {
            var tsk = A(i);
            await Task.Yield();
            GC.Collect();
            result = await tsk;
        }

        // the result should be 20 * (20 - 1) => 190
        return (int)result - 90;
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static int Test()
    {
        return (int)AsyncEntry().Result;
    }
}
