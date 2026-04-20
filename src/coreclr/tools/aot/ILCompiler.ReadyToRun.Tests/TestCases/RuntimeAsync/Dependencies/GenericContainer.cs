// Dependency library for composite-mode generics-on-async-thunks regression tests.
// Provides:
//   - GenericContainer<T>: a generic type with both a non-generic and a generic
//     async method. The generic-method-on-generic-type combination is the exact
//     scenario called out by the parent PR's description as the case that
//     originally broke MethodWithToken/OwningType resolution when emitting async
//     thunks in composite mode.
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

public class GenericContainer<T>
{
    private readonly T _value;

    public GenericContainer(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<T> GetValueAsync()
    {
        await Task.Yield();
        return _value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<string> CombineAsync<U>(U seed)
    {
        await Task.Yield();
        return $"{_value}+{seed}";
    }
}
