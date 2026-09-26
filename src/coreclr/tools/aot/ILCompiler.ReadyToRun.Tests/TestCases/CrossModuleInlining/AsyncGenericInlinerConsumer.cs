using System.Runtime.CompilerServices;
using System.Threading.Tasks;

/// <summary>
/// Consumer that awaits a runtime-async method on a generic type from AsyncCrossModuleGenericLib,
/// instantiated over a value type defined in this assembly so that the async variant of
/// AsyncGenericWrapper&lt;LocalAsyncStruct&gt;.InvokeGetValueAsync() is compiled into this image.
/// </summary>

public struct LocalAsyncStruct { public int Value; }

public static class AsyncGenericInlinerConsumer
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> UseAsync()
    {
        var wrapper = new AsyncGenericWrapper<LocalAsyncStruct>(new LocalAsyncStruct { Value = 1 });
        return await wrapper.InvokeGetValueAsync();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static async Task<int> AwaitValueTask()
    {
        var wrapper = new AsyncGenericWrapper<LocalAsyncStruct>(new LocalAsyncStruct { Value = 2 });
        return await wrapper.GetValueTask();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Task<int> ReturnValueTask()
    {
        var wrapper = new AsyncGenericWrapper<LocalAsyncStruct>(new LocalAsyncStruct { Value = 3 });
        return wrapper.GetValueTask();
    }
}
