// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

public interface IMissingSignature<T>
{
    MissingSignatureType GetMissingType() => null;
}

public class MissingSignatureImplementation<T, U> : IMissingSignature<T>
{
}

public class MissingSignatureFactory<T, U>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public RuntimeTypeHandle GetTypeHandle() => typeof(MissingSignatureImplementation<T, U>).TypeHandle;
}

public static class EntryPoints
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MissingSignatureFactory<T, U> CreateFactory<T, U>() => new MissingSignatureFactory<T, U>();

    public static RuntimeTypeHandle GetTypeHandle() => CreateFactory<string, int>().GetTypeHandle();

    public static int CompilableMethod() => 42;
}
