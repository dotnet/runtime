// Test: Non-composite consumer of composite async methods.
// Calls AsyncCompositeLib methods and has its own async methods,
// exercising multi-step compilation (composite then non-composite)
// with runtime-async in both steps.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class MultiStepAsyncConsumer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> ConsumeCompositeAsync()
    {
        return await AsyncCompositeLib.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> ConsumeCompositeStringAsync()
    {
        return await AsyncCompositeLib.GetStringAsync();
    }
}
