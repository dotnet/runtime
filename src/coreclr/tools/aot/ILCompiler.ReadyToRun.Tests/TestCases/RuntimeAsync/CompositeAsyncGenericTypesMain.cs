// Test: Composite-mode async thunk emission for async methods on generic types
// (and generic async methods on generic types).
//
// Regression coverage for the MethodWithToken/OwningType handling described in
// the parent PR ("Enable compilation of async thunks in composite mode") and
// for the follow-up "Get IL for the (possibly instantiated) method, not the
// definition" fix in ReadyToRunCodegenCompilation.cs. Both code paths only run
// for instantiated methods on instantiated generic types compiled inside a
// composite image.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public static class CompositeAsyncGenericTypesMain
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> CallGenericContainerInt()
    {
        var c = new GenericContainer<int>(42);
        return await c.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericContainerString()
    {
        var c = new GenericContainer<string>("hello");
        return await c.GetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericMethodOnGenericTypeIntLong()
    {
        var c = new GenericContainer<int>(7);
        return await c.CombineAsync<long>(11L);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<string> CallGenericMethodOnGenericTypeStringObject()
    {
        var c = new GenericContainer<string>("k");
        return await c.CombineAsync<object>("v");
    }
}
