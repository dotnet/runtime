// Test: Basic cross-module inlining
// Validates that crossgen2 with --opt-cross-module produces CHECK_IL_BODY fixups
// for methods inlined from InlineableLib into this main assembly.
using System;
using System.Runtime.CompilerServices;

public static class BasicInlining
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestGetValue()
    {
        return InlineableLib.GetValue();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string TestGetString()
    {
        return InlineableLib.GetString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestAdd()
    {
        return InlineableLib.Add(10, 32);
    }
}
