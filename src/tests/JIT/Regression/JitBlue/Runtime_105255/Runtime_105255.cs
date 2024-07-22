// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

#nullable disable

public class Runtime_105255_A
{
    private static Vector512<uint> s_v512_uint_62 = Vector512<uint>.Zero;

    public void Method0()
    {
        s_v512_uint_62 = Vector512.LessThan<uint>(s_v512_uint_62, Vector512<uint>.Zero);
        try
        {
        }
        finally
        {
            for (int i = 0; i < 1; i++) ;
        }
    }

    [Fact]
    public static void TestEntryPoint() => new Runtime_105255_A().Method0();

    /*
    Assert failure(PID 5828 [0x000016c4], Thread: 6044 [0x179c]): Assertion failed '((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) == 0) && "ERROR: Already morphed this node!"' in 'TestClass:Method0():this' during 'Morph - Global' (IL size 22846; hash 0x46e9aa75; Tier0-FullOpts)
        File: D:\a\_work\1\s\src\coreclr\jit\morph.cpp:12227
        Image: C:\h\w\A715090A\p\CoreRoot\corerun.exe

    Assertion failed '((tree->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) == 0) && "ERROR: Already morphed this node!"' during 'Morph - Global'
    */
}
