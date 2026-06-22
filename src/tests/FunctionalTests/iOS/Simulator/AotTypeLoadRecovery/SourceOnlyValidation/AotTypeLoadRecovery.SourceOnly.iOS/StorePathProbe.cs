using System.Runtime.CompilerServices;
using ReproCore;

namespace AotTypeLoadRecovery.SourceOnly.iOS;

internal static class StorePathProbe
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RootWithoutRunning()
    {
        StorePathHarness.RootWithoutRunning();
    }
}
