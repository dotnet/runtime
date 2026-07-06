// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Runtime.CompilerServices;
using Xunit;

// These tests verify that the JIT stack-allocates non-escaping, constant-length
// strings. When a string is stack-allocated the underlying String.FastAllocateString
// call is removed, so its absence in the disassembly is what we check for.

public class StackAllocatedStrings
{
    // char.ToString() lowers to string.CreateFromChar(c) => FastAllocateString(1).
    // The one-char string does not escape, so it is stack-allocated.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static char CharInstanceToString(char c)
    {
        // X64-NOT: FastAllocateString
        // ARM64-NOT: FastAllocateString
        string s = c.ToString();
        return s[0];
    }

    // Same underlying path via the static char.ToString(char) overload.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CharStaticToString(char c)
    {
        // X64-NOT: FastAllocateString
        // ARM64-NOT: FastAllocateString
        string s = char.ToString(c);
        return s.Length;
    }

    // Escape analysis sees through String equality, so the temporary string
    // produced by ToString() still does not escape and is stack-allocated.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool CharToStringEquals(char c)
    {
        // X64-NOT: FastAllocateString
        // ARM64-NOT: FastAllocateString
        string s = c.ToString();
        return s == "x";
    }

    // Rune.ToString() calls FastAllocateString with a constant length on both the
    // BMP (length 1) and the surrogate-pair (length 2) paths; neither escapes.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int RuneToString(Rune r)
    {
        // X64-NOT: FastAllocateString
        // ARM64-NOT: FastAllocateString
        string s = r.ToString();
        return s.Length;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (CharInstanceToString('q') != 'q')
            return 0;

        if (CharStaticToString('a') != 1)
            return 0;

        if (CharToStringEquals('x') != true)
            return 0;

        if (CharToStringEquals('y') != false)
            return 0;

        // BMP scalar: one UTF-16 code unit.
        if (RuneToString(new Rune('z')) != 1)
            return 0;

        // Supplementary scalar: surrogate pair, two UTF-16 code units.
        if (RuneToString(new Rune(0x1F600)) != 2)
            return 0;

        return 100;
    }
}
