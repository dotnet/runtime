// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public struct LocalStruct
{
    public int Value;
}

public sealed class LocalSealedClass
{
    public int Value;
}

public sealed class LocalDerivedFromExternal : ExternalLib.ExternalGenericBase<LocalDerivedFromExternal>
{
    public int Value;
}

public interface ICheckComparer<T>
{
    void Check();
}

public static class ComparerFolding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool LocalStructDefaultComparerIsExact() =>
        ((object)EqualityComparer<LocalStruct>.Default) is ICheckComparer<LocalStruct>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool LocalClassDefaultComparerIsExact() =>
        ((object)EqualityComparer<LocalSealedClass>.Default) is ICheckComparer<LocalSealedClass>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool LocalDerivedFromExternalDefaultComparerIsExact() =>
        ((object)EqualityComparer<LocalDerivedFromExternal>.Default) is ICheckComparer<LocalDerivedFromExternal>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ExternalStructDefaultComparerIsExact() =>
        ((object)EqualityComparer<ExternalLib.ExternalStruct>.Default) is ICheckComparer<ExternalLib.ExternalStruct>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ExternalClassDefaultComparerIsExact() =>
        ((object)EqualityComparer<ExternalLib.ExternalType>.Default) is ICheckComparer<ExternalLib.ExternalType>;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool LocalStructDefaultComparerEquals() =>
        EqualityComparer<LocalStruct>.Default.Equals(default, default);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool ExternalStructDefaultComparerEquals() =>
        EqualityComparer<ExternalLib.ExternalStruct>.Default.Equals(default, default);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Create")]
    private static extern EqualityComparer<LocalStruct> CreateLocalStructComparer(EqualityComparer<LocalStruct>? _);

    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = "Create")]
    private static extern EqualityComparer<ExternalLib.ExternalStruct> CreateExternalStructComparer(EqualityComparer<ExternalLib.ExternalStruct>? _);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object LocalStructEqualityComparerCreate() =>
        CreateLocalStructComparer(null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object ExternalStructEqualityComparerCreate() =>
        CreateExternalStructComparer(null);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SharedGenericSpanIndexOf<T>(ReadOnlySpan<T> span, T value) where T : IEquatable<T>? =>
        span.IndexOf(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int CallSharedGenericSpanIndexOf(ReadOnlySpan<string> span, string value) =>
        SharedGenericSpanIndexOf(span, value);
}
