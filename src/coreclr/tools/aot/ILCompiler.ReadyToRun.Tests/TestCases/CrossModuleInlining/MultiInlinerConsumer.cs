/// <summary>
/// Consumer that uses two different generic types from CrossModuleGenericLib,
/// each instantiated with a value type defined in this assembly.
/// Value types are required because CrossModuleCompileable discovery uses
/// CanonicalFormKind.Specific, which preserves value type arguments (unlike
/// reference types which become __Canon, losing the alternate location info).
///
/// GenericWrapperA&lt;LocalStruct&gt;.InvokeGetValue() and GenericWrapperB&lt;LocalStruct&gt;.InvokeGetValue()
/// are two distinct MethodDefs that each inline Utility.GetValue(), producing
/// multiple cross-module inliner entries for the same inlinee in CrossModuleInlineInfo.
/// </summary>

public struct LocalStruct { public int Value; }

public static class MultiInlinerConsumer
{
    public static int UseA()
    {
        var wrapper = new GenericWrapperA<LocalStruct>(new LocalStruct { Value = 1 });
        return wrapper.InvokeGetValue();
    }

    public static int UseB()
    {
        var wrapper = new GenericWrapperB<LocalStruct>(new LocalStruct { Value = 2 });
        return wrapper.InvokeGetValue();
    }
}
