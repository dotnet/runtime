// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly DevDiv_279396 {}
.module DevDiv_279396.exe

// This test originally triggered an assertion in the emitter that ensured that no lclVar or lclFld access exceeded the
// bounds of its containing method's frame. The problematic access was created during morphing by folding
// `(ind long (addr int (lclVar int V0)))` into `(lclFld long V0 0)`. This corresponds to the body of `C::Test` below.

.class private abstract auto ansi sealed beforefieldinit C extends [mscorlib]System.Object
{
    .method private static int64 Test(int32 i) noinlining
    {
        ldarga.s i
        ldind.i8
        ret
    }

    .method private static int32 Main()
    {
        .entrypoint

        ldc.i4 100
        dup
        call int64 C::Test(int32)
        conv.i4
        bne.un.s fail

        ldc.i4 100
        ret

fail:
        ldc.i4 101
        ret
    }
}
