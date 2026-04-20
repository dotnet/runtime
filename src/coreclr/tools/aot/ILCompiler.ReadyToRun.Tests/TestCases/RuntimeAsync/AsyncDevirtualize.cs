// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public interface IAsyncService
{
    Task<int> GetValueAsync();
}

public class OpenImpl : IAsyncService
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 10;
    }
}

public sealed class SealedImpl : IAsyncService
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 20;
    }
}

public static class AsyncDevirtualize
{
    // Sealed type known at compile time — should devirtualize
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnSealed(SealedImpl obj)
    {
        return await obj.GetValueAsync();
    }

    // newobj gives exact type info — should devirtualize through resolveVirtualMethod
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnNewOpen()
    {
        IAsyncService svc = new OpenImpl();
        return await svc.GetValueAsync();
    }

    // Generic constrained dispatch
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGenericConstrained<T>(T obj) where T : IAsyncService
    {
        return await obj.GetValueAsync();
    }
}
