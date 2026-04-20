// Interface + two implementations (sealed and non-sealed/open) of an
// async-returning method. Used by consumers to exercise devirtualization
// of async interface dispatch.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public interface IAsyncService
{
    Task<int> GetValueAsync();
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

public class OpenImpl : IAsyncService
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual async Task<int> GetValueAsync()
    {
        await Task.Yield();
        return 10;
    }
}
