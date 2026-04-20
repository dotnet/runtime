// Calls inlinable helpers in another module that transitively touch types
// and members from a further module, so the consumer's R2R image must
// carry tokens for both the mid-layer and leaf modules.
using System;
using System.Runtime.CompilerServices;

public static class TransitiveReferences
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestTransitiveValue()
    {
        return InlinableLeafCallers.GetExternalValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestNestedTypeAccess()
    {
        return InlinableLeafCallers.GetNestedValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTransitiveTypeCreation()
    {
        return InlinableLeafCallers.CreateExternal();
    }
}
