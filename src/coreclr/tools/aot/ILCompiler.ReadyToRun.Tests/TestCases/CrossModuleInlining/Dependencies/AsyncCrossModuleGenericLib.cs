using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Library with a generic type whose runtime-async method inlines a utility method.
/// When the consumer awaits the method on an instantiation over one of its own value types,
/// crossgen2 compiles the async variant of the method into the consumer as a CrossModuleCompileable
/// generic, making the async variant a cross-module inliner of AsyncGenericUtility.GetAsyncGenericValue.
/// </summary>
public static class AsyncGenericUtility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetAsyncGenericValue() => 42;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetSharedInlineeValue() => 43;
}

public class AsyncGenericWrapper<T>
{
    private T _value;

    public AsyncGenericWrapper(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<int> InvokeGetValueAsync()
    {
        await Task.Yield();
        return AsyncGenericUtility.GetAsyncGenericValue();
    }

    // Not async, so both the task-returning method and its async variant are compiled from this IL.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task<int> GetValueTask()
    {
        return Task.FromResult(AsyncGenericUtility.GetSharedInlineeValue());
    }
}
