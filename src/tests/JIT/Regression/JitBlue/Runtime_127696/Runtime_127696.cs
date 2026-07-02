// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/127696
//
// On Linux x64 (UNIX_AMD64_ABI), constructing Nullable<T> where T is a readonly record struct
// that implements a recursive generic interface (e.g. IEntityId<T> referencing EntityIdValue<T>)
// caused a SIGSEGV during type loading. The SysV AMD64 ABI struct-register-classification
// code (ClassifyEightBytesWithManagedLayout / HasImpliedRepeatedFields) called GetSize() via
// DontLoadTypes on a value-type field whose exact generic instantiation had been canonicalized
// to __Canon during recursive layout loading — the type was not in the hash, so the lookup
// returned a null TypeHandle, which triggered an assert or a crash.

using System.Runtime.CompilerServices;
using Xunit;

interface IEntityId<T> where T : struct, IEntityId<T>
{
    EntityIdValue<T> Value { get; }
    static abstract T From(EntityIdValue<T> value);
}

readonly record struct EntityIdValue<T>(int Id) where T : struct, IEntityId<T>;

readonly record struct UserId(EntityIdValue<UserId> Value) : IEntityId<UserId>
{
    public static UserId From(EntityIdValue<UserId> value) => new(value);
}

public class Runtime_127696
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static UserId? GetUserId(bool hasValue) => hasValue ? new UserId(new EntityIdValue<UserId>(42)) : null;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ConsumeNullable(UserId? id) => id?.Value.Id ?? -1;

    [Fact]
    public static void TestEntryPoint()
    {
        UserId? present = GetUserId(true);
        UserId? absent = GetUserId(false);

        int a = ConsumeNullable(present);
        int b = ConsumeNullable(absent);

        Assert.Equal(42, a);
        Assert.Equal(-1, b);
    }
}
