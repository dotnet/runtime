// Dependency library: defines an async interface and sealed implementation
// for cross-module async devirtualization tests in composite mode.
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
