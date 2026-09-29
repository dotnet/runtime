using System.Collections.Generic;

namespace Microsoft.DotNet.HotReload.Utils.Generator.Util;
public static class AsyncEnumerableExtras {
    public async static IAsyncEnumerable<T> Empty<T> () {
        await System.Threading.Tasks.Task.CompletedTask;
        yield break;
    }
}
