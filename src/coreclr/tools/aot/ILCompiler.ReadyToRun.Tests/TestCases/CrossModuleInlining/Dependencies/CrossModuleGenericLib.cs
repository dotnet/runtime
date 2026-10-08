using System.Runtime.CompilerServices;

/// <summary>
/// Library with two generic types that each inline the same utility method.
/// When compiled via CrossModuleCompileable generics, each type's InvokeGetValue()
/// becomes a distinct cross-module inliner MethodDef for the same inlinee (Utility.GetValue),
/// producing multiple cross-module inliner entries in the CrossModuleInlineInfo section.
/// </summary>
public static class Utility
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetValue() => 42;
}

public class GenericWrapperA<T>
{
    private T _value;

    public GenericWrapperA(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int InvokeGetValue()
    {
        return Utility.GetValue();
    }
}

public class GenericWrapperB<T>
{
    private T _value;

    public GenericWrapperB(T value) => _value = value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public virtual int InvokeGetValue()
    {
        return Utility.GetValue();
    }
}
