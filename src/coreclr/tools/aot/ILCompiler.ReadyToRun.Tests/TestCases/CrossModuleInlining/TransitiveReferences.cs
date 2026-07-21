// Test: Transitive cross-module references
// Validates that when InlineableLibTransitive is inlined, its references to ExternalLib
// are properly encoded in the R2R image (requiring tokens for both libraries).
using System;
using System.Runtime.CompilerServices;

public static class TransitiveReferences
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestTransitiveValue()
    {
        return InlineableLibTransitive.GetExternalValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestNestedTypeAccess()
    {
        return InlineableLibTransitive.GetNestedValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static object TestTransitiveTypeCreation()
    {
        return InlineableLibTransitive.CreateExternal();
    }
}
