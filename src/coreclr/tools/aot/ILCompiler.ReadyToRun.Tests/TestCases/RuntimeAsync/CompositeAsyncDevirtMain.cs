// Test: Composite mode async devirtualization across module boundaries.
// Interface defined in AsyncInterfaceLib, call sites here.
// In composite mode, crossgen2 should devirtualize sealed type dispatch.
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
