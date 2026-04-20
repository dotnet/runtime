// Calls an async interface method via three dispatch shapes: sealed type,
// newobj-then-interface, and generic-constrained. Consumers of
// InterfaceAndImpls to exercise devirtualization of async dispatch.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AwaitsThroughInterface
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnSealed(SealedImpl obj)
    {
        return await obj.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallOnNewOpen()
    {
        IAsyncService svc = new OpenImpl();
        return await svc.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGenericConstrained<T>(T obj) where T : IAsyncService
    {
        return await obj.GetValueAsync();
    }
}
