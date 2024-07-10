// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InvalidCSharp;

using Xunit;

public class Validate
{
    [Fact]
    public static void Validate_Activation()
    {
        Console.WriteLine($"{nameof(Validate_Activation)}...");

        Assert.Equal("System.Span<Int32>[0]", Activator.CreateInstance<Span<int>>().ToString());
        Assert.Equal("System.Span<String>[0]", Activator.CreateInstance<Span<string>>().ToString());
        Assert.Equal("System.ReadOnlySpan<Int32>[0]", Activator.CreateInstance<ReadOnlySpan<int>>().ToString());
        Assert.Equal("System.ReadOnlySpan<String>[0]", Activator.CreateInstance<ReadOnlySpan<string>>().ToString());
    }

    [Fact]
    public static void Validate_TypeLoad()
    {
        Console.WriteLine($"{nameof(Validate_TypeLoad)}...");

        Console.WriteLine($" -- Instantiate: {Exec.GenericClass()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericInterface()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericValueType()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericByRefLike()}");
        Console.WriteLine($" -- Instantiate: {Exec.GenericByRefLike_ConstraintsAreIndependent_Int32_Int32()}");
        Console.WriteLine($" -- Create: {Exec.CreateDefaultInstance()}");

        Assert.Throws<TypeLoadException>(() => { Exec.GenericClass_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericInterface_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericValueType_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericByRefLike_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericByRefLike_ConstraintsAreIndependent_Interface_ByRefLike_Invalid(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericByRefLike_ConstraintsAreIndependent_ByRefLike_ByRefLike_Invalid(); });
    }

    [Fact]
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

    interface I1 { }

    struct S {}
    struct S<T> {}
    struct S_I1 : I1 {}
    struct S_I1<T> : I1 {}
    struct S_DI1 : InvalidCSharp.DefaultInterface {}
    struct S_DI2 : InvalidCSharp.DefaultInterface { public int Method() => 1; }

    ref struct RS { }
    ref struct RS<T> { }
    ref struct RS_I1 : I1 { }
    ref struct RS_I1<T> : I1 { }
    // ref struct RS_DI1 - See InvalidCSharp.il
    // ref struct RS_DI2 - See InvalidCSharp.il

    sealed class Ignored { }

    [Fact]
    public static void Validate_RecognizedOpCodeSequences()
    {
        Console.WriteLine($"{nameof(Validate_RecognizedOpCodeSequences)}...");

        Exec.BoxUnboxAny();
        Exec.BoxBranch();
        Exec.BoxIsinstUnboxAny();

        Exec.BoxIsinstBranchVarious();

        Assert.True(Exec.BoxIsinstBranch<int, object>(default));
        Assert.False(Exec.BoxIsinstBranch<int, I1>(default));
        Assert.False(Exec.BoxIsinstBranch<object, I1>(default));

        Assert.True(Exec.BoxIsinstBranch<S, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S<int>, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S<object>, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S, S>(default));
        Assert.True(Exec.BoxIsinstBranch<S<int>, S<int>>(default));
        Assert.True(Exec.BoxIsinstBranch<S<object>, S<object>>(default));
        Assert.False(Exec.BoxIsinstBranch<S, I1>(default));
        Assert.False(Exec.BoxIsinstBranch<S<int>, I1>(default));
        Assert.False(Exec.BoxIsinstBranch<S<object>, I1>(default));

        Assert.True(Exec.BoxIsinstBranch<S_I1, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<int>, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<object>, object>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1, S_I1>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<int>, S_I1<int>>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<object>, S_I1<object>>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1, I1>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<int>, I1>(default));
        Assert.True(Exec.BoxIsinstBranch<S_I1<object>, I1>(default));

        Assert.Equal($"{nameof(Validate)}+{nameof(S)}", Exec.ConstrainedCallVirtToString<S>(new S()));
        Assert.Equal(0, Exec.ConstrainedCallVirtMethod<S_DI1>(new S_DI1()));
        Assert.Equal(1, Exec.ConstrainedCallVirtMethod<S_DI2>(new S_DI2()));
        Assert.Equal(1, Exec.ConstrainedCallVirtMethod<RS_DI2>(new RS_DI2()));

        Assert.Equal(-1, Exec.ConstrainedCallVirtMethod<S_DI1>(new S_DI1(), skipCall: true));
        Assert.Equal(-1, Exec.ConstrainedCallVirtMethod<S_DI2>(new S_DI2(), skipCall: true));
        Assert.Equal(-1, Exec.ConstrainedCallVirtMethod<RS_DI2>(new RS_DI2(), skipCall: true));
    }

    [Fact]
    public static void Validate_RecognizedOpCodeSequences_Mismatch()
    {
        Console.WriteLine($"{nameof(Validate_RecognizedOpCodeSequences_Mismatch)}...");

        // box !T ; isinst S ; unbox.any S should always be guarded by a box !T ; isinst S;
        // brtrue/brfalse branch, so if it's ever executed and the types aren't equal that's invalid
        Assert.Throws<InvalidProgramException>(() => { Exec.BoxIsinstUnboxAny_Mismatch(); });
    }

    [Fact]
    public static void Validate_InvalidOpCode_Scenarios()
    {
        Console.WriteLine($"{nameof(Validate_InvalidOpCode_Scenarios)}...");

        // These methods uses opcodes that are not able to handle ByRefLike type operands.
        // The TypeLoader prevents these invalid types from being constructed. We rely on
        // the failure to construct these invalid types to block opcode usage.
        Assert.Throws<TypeLoadException>(() => { Exec.AllocArray<RS>(); });
        Assert.Throws<TypeLoadException>(() => { Exec.AllocMultiDimArray<RS>(); });
        Assert.Throws<TypeLoadException>(() => { Exec.GenericClassWithStaticField_Invalid(); });

        // Test that explicitly tries to box a ByRefLike type.
        Assert.Throws<InvalidProgramException>(() => { Exec.BoxAsObject<RS>(new RS()); });

        // Test that implicitly tries to box a ByRefLike type.
        Assert.Throws<InvalidProgramException>(() => { Exec.ConstrainedCallVirtToString<RS>(new RS()); });
        Assert.Throws<InvalidProgramException>(() => { Exec.ConstrainedCallVirtMethod<RS_DI1>(new RS_DI1()); });
        Assert.Throws<InvalidProgramException>(() => { Exec.ConstrainedCallVirtMethod<RS_DI1>(new RS_DI1(), skipCall: false); });
        Assert.Throws<InvalidProgramException>(() => { Exec.ConstrainedCallVirtMethod<RS_DI1>(new RS_DI1(), skipCall: true); });
    }

    [Fact]
    public static void Validate_Inlining_Behavior()
    {
        Console.WriteLine($"{nameof(Validate_Inlining_Behavior)}...");

        // The call here is to ensure that an inlinable method that isn't called
        // due to a branch, see input argument, doesn't break the compilation of the
        // rest of method that is executed.
        Assert.True(Exec.TypeLoadExceptionAvoidsInline(false));
    }

    // [Fact]
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
