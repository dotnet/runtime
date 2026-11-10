// Runtime-async caller for the composite devirt regression test. Awaits non-runtime-async virtuals
// (in AsyncDevirtNonAsyncCalleeLib) that the JIT devirtualizes to the sealed receiver, requesting the
// callee's synthesized async-variant thunk.
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncDevirtNonAsyncCallee
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task AwaitInheritedValueTask(Holder h)
    {
        await h.Writer.CompleteValueTaskAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task AwaitInheritedTask(Holder h)
    {
        await h.Writer.CompleteTaskAsync();
    }
}
