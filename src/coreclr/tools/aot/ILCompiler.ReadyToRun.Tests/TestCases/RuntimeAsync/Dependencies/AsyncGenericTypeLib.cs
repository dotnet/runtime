// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class GenericContainer<T>
{
    private readonly T _value;

    public GenericContainer(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GetValueAsync()
    {
        await Task.Yield();
        return _value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> CombineAsync<U>(U seed)
    {
        await Task.Yield();
        return $"{_value}+{seed}";
    }
}
