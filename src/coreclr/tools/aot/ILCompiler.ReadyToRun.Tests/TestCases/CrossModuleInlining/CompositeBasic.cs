// Test: Composite mode basic compilation
// Validates that composite mode R2R compilation with multiple assemblies
// produces correct manifest references and component assembly entries.
using System;
using System.Runtime.CompilerServices;

public static class CompositeBasic
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestCompositeCall()
    {
        return CompositeLib.GetCompositeValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestCompositeTypeCreation()
    {
        return new CompositeLib.CompositeType();
    }
}
