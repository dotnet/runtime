// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncDevirtMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnSealed(SealedAsyncService svc)
    {
        return await svc.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnNewOpen()
    {
        IAsyncCompositeService svc = new OpenAsyncService();
        return await svc.GetValueAsync();
    }
}
