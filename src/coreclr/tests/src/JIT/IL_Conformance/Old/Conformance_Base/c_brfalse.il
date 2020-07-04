// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.class _brfalse {
.method public static  int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack               20
.locals (int32,int32)
        ldc.i4          0x0
        dup
        stloc   0
        stloc   1

BEGIN:
        ldc.i4          0x1
        ldloc   0
        add
        dup
        stloc   0
        ldc.i4          0x1
        ceq
        ldloc   1
        add
        stloc   1

        ldc.i4          0x0
        brfalse         TAG3


TAG1:
        ldc.i4          0x2
        ldloc   0
        add
        dup
        stloc   0
        ldc.i4          0xB
        ceq
        ldloc   1
        add
        stloc   1

        ldc.i4          0x1
        brfalse         END
        ldc.i4          0x0
        brfalse         0


TAG2:
        ldc.i4          0x4
        ldloc   0
        add
        dup
        stloc   0
        ldc.i4          0xF
        ceq
        ldloc   1
        add
        stloc   1

        ldc.i4          0x0
        brfalse         BOUNDS_TEST


TAG3:
        ldc.i4          0x8
        ldloc   0
        add
        dup
        stloc   0
        ldc.i4          0x9
        ceq
        ldloc   1
        add
        stloc   1

        ldc.i4          0x1
        brfalse         END
        ldc.i4          0x0
        brfalse         TAG1

BOUNDS_TEST:
        ldc.i4          0xFFFFFFFF
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1

        ldc.i4          0x7FFFFFFF
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1

        ldc.i4          0x80000000
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1

        ldc.i4          0x55555555
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1

        ldc.i4          0xAAAAAAAA
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1

        ldc.i4          0x00000000
        brfalse         END
        ldc.i4          0x1
        ldloc   1
        add
        stloc   1


END:
        ldloc   1
        ldc.i4          0x00000009
        ceq
        brfalse         FAIL
PASS:
        ldc.i4 100
        ret
FAIL:
        ldc.i4          0x0
        ret

}
}
.assembly c_brfalse.exe {}
