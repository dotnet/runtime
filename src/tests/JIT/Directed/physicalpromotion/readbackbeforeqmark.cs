// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_87508
{
    [Fact]
    public static int TestEntryPoint()
    {
        return new Runtime_87508().WriteBlock("1234");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public int WriteBlock(string source)
    {
        ReadOnlySpan<char> line = GetNextLine();
        Trash();
        // Unrolling of this creates a QMARK with LCL_FLD uses in the arms. The
        // JIT must be careful to read the fields of the promoted 'line' back
        // before the conditional nature of the QMARK.
        if (line.StartsWith("{"))
        {
            Console.WriteLine("FAIL: succeeded");
            return -1;
        }

        if (!Unsafe.AreSame(ref MemoryMarshal.GetReference(line), ref MemoryMarshal.GetArrayDataReference(_emptyChars)))
        {
            Console.WriteLine("FAIL: References were not equal");
            return -2;
        }

        Console.WriteLine("PASS");
        return 100;
    }

    private char[] _emptyChars = new char[0];

    [MethodImpl(MethodImplOptions.NoInlining)]
    private ReadOnlySpan<char> GetNextLine()
    {
        return _emptyChars;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private nint Trash() => 0;
}
