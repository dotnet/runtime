// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_54100
{
    // The test ends up containing an empty try block and we do not find a 
    // non-empty block from which a treeNode can be extracted to use it for 
    // creating zero-init refPositions.
    
    static ushort[][] s_23 = new ushort[][]{new ushort[]{0}};
    static short s_32;
    static short s_33;
    static int s_45;
    [Fact]
    public static int TestEntryPoint()
    {
        ushort[] vr4 = s_23[0];
        return (int)M45();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static ushort M45()
    {
        short var0;
        try
        {
            var0 = s_32;
        }
        finally
        {
            var0 = s_33;
            int var1 = s_45;
            ulong vr8 = default(ulong);
            var0 = (short)((sbyte)vr8 - var0);
            try
            {
                M46();
            }
            finally
            {
                M46();
            }

            System.Console.WriteLine(var1);
        }

        return 100;
    }

    static ulong M46()
    {
        return default(ulong);
    }
}
