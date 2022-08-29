// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Xunit;

public unsafe class ManagedPointers
{
    [Fact]
    public static void Validate_BoxingHelpers_NullByRef()
    {
        Console.WriteLine($"Running {nameof(Validate_BoxingHelpers_NullByRef)}...");
        Assert.Throws<NullReferenceException>(() =>
        {
            object boxed = Unsafe.NullRef<int>();
        });
        Assert.Throws<NullReferenceException>(() =>
        {
            object boxed = Unsafe.NullRef<Guid>();
        });
        Assert.Throws<NullReferenceException>(() =>
        {
            object boxed = Unsafe.NullRef<string>();
        });
    }

    [Fact]
    public static void Validate_GeneratedILStubs_NullByRef()
    {
        Console.WriteLine($"Running {nameof(Validate_GeneratedILStubs_NullByRef)}...");
        {
            var fptr = (delegate*unmanaged<ref int, nint>)(delegate*unmanaged<void*, nint>)&PassByRef;
            Assert.Equal(0, fptr(ref Unsafe.NullRef<int>()));
        }

        {
            var fptr = (delegate*unmanaged<ref Guid, nint>)(delegate*unmanaged<void*, nint>)&PassByRef;
            Assert.Equal(0, fptr(ref Unsafe.NullRef<Guid>()));
        }

        Assert.Throws<NullReferenceException>(() =>
        {
            var fptr = (delegate*unmanaged<ref string, nint>)(delegate*unmanaged<void*, nint>)&PassByRef;
            fptr(ref Unsafe.NullRef<string>());
        });

        [UnmanagedCallersOnly]
        static nint PassByRef(void* a) => (nint)a;
    }

    [Fact]
    public static void Validate_IntrinsicMethodsWithByRef_NullByRef()
    {
        Console.WriteLine($"Running {nameof(Validate_IntrinsicMethodsWithByRef_NullByRef)}...");

        Assert.Throws<NullReferenceException>(() => Interlocked.Increment(ref Unsafe.NullRef<int>()));
        Assert.Throws<NullReferenceException>(() => Interlocked.Increment(ref Unsafe.NullRef<long>()));
        Assert.Throws<NullReferenceException>(() => Interlocked.Decrement(ref Unsafe.NullRef<int>()));
        Assert.Throws<NullReferenceException>(() => Interlocked.Decrement(ref Unsafe.NullRef<long>()));

        Assert.Throws<NullReferenceException>(() => Interlocked.And(ref Unsafe.NullRef<int>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.And(ref Unsafe.NullRef<long>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Or(ref Unsafe.NullRef<int>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Or(ref Unsafe.NullRef<long>(), 0));

        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<int>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<long>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<float>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<double>(), 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange(ref Unsafe.NullRef<object>(), new object()));
        Assert.Throws<NullReferenceException>(() => Interlocked.Exchange<object>(ref Unsafe.NullRef<object>(), new object()));

        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<int>(), 0, 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<long>(), 0, 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<float>(), 0, 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<double>(), 0, 0));
        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange(ref Unsafe.NullRef<object>(), new object(), new object()));
        Assert.Throws<NullReferenceException>(() => Interlocked.CompareExchange<object>(ref Unsafe.NullRef<object>(), new object(), new object()));
    }
}