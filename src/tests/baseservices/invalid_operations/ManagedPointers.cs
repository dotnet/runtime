// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
}