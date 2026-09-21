// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Note: This test file is the source of the Runtime_70607.il file. It requires
// InlineIL.Fody to compile. It is not used as anything but a reference of that
// IL file.

using InlineIL;
using System;
using System.Runtime.CompilerServices;

public class Runtime_70607
{
    public static int Main()
    {
        int result = M(0);
        if (result == 255)
        {
            Console.WriteLine("PASS: Result was 255");
            return 100;
        }

        Console.WriteLine("FAIL: Result was {0}", result);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int M(byte arg0)
    {
        IL.Emit.Ldc_I4(-1);
        IL.Emit.Starg(0);
        IL.Emit.Ldarg(0);
        return IL.Return<int>();
    }
}
