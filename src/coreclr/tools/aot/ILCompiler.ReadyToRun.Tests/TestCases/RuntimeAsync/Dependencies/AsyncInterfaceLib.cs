// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public interface IAsyncCompositeService
{
    Task<int> GetValueAsync();
}

public sealed class SealedAsyncService : IAsyncCompositeService
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 42;
    }
}

public class OpenAsyncService : IAsyncCompositeService
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 10;
    }
}
