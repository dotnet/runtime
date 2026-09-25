// Test: Async methods with multiple suspension points.
// Used to validate that ResumptionStubEntryPoint fixups are deduplicated
// across compilation retries (only one fixup per compiled method).
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class AsyncMultipleSuspensionPoints
{
    private class VirtualTarget
    {
        public virtual int Transform(int value) => value;
    }

    private sealed class DerivedVirtualTarget : VirtualTarget
    {
        public override int Transform(int value) => value + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CallVirtual(VirtualTarget target, int value) => target.Transform(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> MultipleAwaits()
    {
        int x = CallVirtual(new DerivedVirtualTarget(), 0);
        await Task.Yield();
        x++;
        await Task.Yield();
        x++;
        await Task.Yield();
        return x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> MultipleAwaitsWithRefs()
    {
        string a = "a";
        await Task.Yield();
        string b = a + "b";
        await Task.Yield();
        string c = b + "c";
        await Task.Yield();
        return c;
    }
}
