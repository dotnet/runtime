// Test: LSE code is guarded per method on Linux ARM64.
using System.Threading;

public static class Arm64Atomics
{
    public static int InterlockedOr(ref int location, int value)
    {
        return Interlocked.Or(ref location, value);
    }
}
