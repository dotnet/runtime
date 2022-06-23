// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharp;

using Xunit;

class Validate
{
    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_TypeLoad()
    {
        Console.WriteLine($"{nameof(Validate_TypeLoad)}...");

        Console.WriteLine($" -- Instantiate: {Exec.GenericClass()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericInterface()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericValueType()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericByRefLike()}");

        Assert.Throws<TypeLoadException>(() => { Exec.GenericClass_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericInterface_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericValueType_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericByRefLike_Invalid(); });
    }

    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_Casting_Scenarios()
    {
        Console.WriteLine($"{nameof(Validate_Casting_Scenarios)}...");

        // Opcodes that can handle cases naturally for ByRefLike types should fail.
        // Since ByRefLike types can never be boxed, it stands to reason attempting
        // to cast an object to a ByRefLike type will always return null or throw an
        // appropriate exception.
        Assert.False(Exec.InstanceOfT(new object()));
        Assert.Throws<InvalidCastException>(() => { Exec.CastToT(new object()); });
        Assert.Throws<InvalidCastException>(() => { Exec.UnboxToT(new object()); });
    }

    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_RecognizedOpCodeSequences_Scenarios()
    {
        Console.WriteLine($"{nameof(Validate_RecognizedOpCodeSequences_Scenarios)}...");

        Assert.True(Exec.BoxUnboxAny());
        Assert.True(Exec.BoxBranch());
        Assert.True(Exec.BoxIsinstUnboxAny());
        Assert.True(Exec.BoxIsinstBranch());
    }

    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_InvalidOpCode_Scenarios()
    {
        Console.WriteLine($"{nameof(Validate_InvalidOpCode_Scenarios)}...");

        // These methods uses opcodes that are not able to handle ByRefLike type operands.
        // The TypeLoader prevents these invalid types from being constructed. We rely on
        // the failure to construct these invalid types to block opcode usage.
        Assert.Throws<TypeLoadException>(() => { Exec.AllocArrayOfT_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.AllocMultiDimArrayOfT_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericClassWithStaticField_Invalid(); });

        // Test that explicitly tries to box a ByRefLike type.
        Assert.Throws<InvalidProgramException>(() => { Exec.BoxAsObject(); });
    }

    [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_Inlining_Behavior()
    {
        Console.WriteLine($"{nameof(Validate_Inlining_Behavior)}...");

        // The call here is to ensure that an inlinable method that isn't called
        // due to a branch, see input argument, doesn't break the compilation of the
        // rest of method that is executed.
        Assert.True(Exec.TypeLoadExceptionAvoidsInline(false));
    }

    // [Fact]
    [SkipOnMono("Mono does not support ByRefLike generics yet")]
    public static void Validate_MemberDiscoveryViaReflection_ForSpanReadOnlySpan()
    {
        Console.WriteLine($"{nameof(Validate_MemberDiscoveryViaReflection_ForSpanReadOnlySpan)}...");

        // // Validate specific Span<T> and ReadOnlySpan<T> constructors can be discovered when T is ByRefLike
        // {
        //     var ctor = typeof(Span<ByRefLikeType>).GetConstructor(new[] { typeof(void).MakePointerType(), typeof(int) });
        //     Assert.NotNull(ctor);
        // }

        // {
        //     var ctor = typeof(ReadOnlySpan<ByRefLikeType>).GetConstructor(new[] { typeof(void).MakePointerType(), typeof(int) });
        //     Assert.NotNull(ctor);
        // }

        // // Validate overloaded methods of Span<T> can be discovered when T is ByRefLike
        // {
        //     var m = typeof(Span<ByRefLikeType>).GetMethod("op_Implicit", new[] { typeof(Span<ByRefLikeType>) });
        //     Assert.NotNull(m);
        // }
    }
}