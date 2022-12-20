// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class FoldingExtendsInt32On64BitHostsTest
{
    // On 64 bit hosts, 32 bit constants are stored as 64 bit signed values.
    // gtFoldExpr failed to properly truncate the folded value to 32 bits when
    // the host was 64 bit and the target - 32 bit. Thus local assertion prop
    // got the "poisoned" value, which lead to silent bad codegen.

    public static int Main()
    {
        var r1 = 31;
        // "Poisoned" value.
        var s1 = 0b11 << r1;

        if (s1 == 0b11 << 31)
        {
            return 100;
        }

        // Just so that Roslyn actually uses locals.
        Use(s1);
        Use(r1);

        return -1;
    }

    private static void Use(int a) { }
}
