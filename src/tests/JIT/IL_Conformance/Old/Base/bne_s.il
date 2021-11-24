// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.method public static int32 main(class [mscorlib]System.String[]) {
.locals         (class [mscorlib]System.String)
.entrypoint

        ldc.i4          0x44444444
        ldc.i4          0x55555555
        bne.un.s        next0
        br                      fail
next0:
        ldc.i4          0x66666666
        ldc.i4          0x66666666
        bne.un        fail
        br                      next1
next1:
        ldc.i4          0x55555555
        ldc.i4          0x44444444
        bne.un.s        next2
        br                      fail
next2:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x3F800000)
        bne.un.s        next3
        br                      fail
next3:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        bne.un        fail
        br                      next4
next4:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0xBF800000)
        bne.un.s        next5
        br                      fail
next5:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x3F800000)
        bne.un.s        next6
        br                      fail
next6:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        bne.un.s        fail
        br                      next7
next7:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0xBF800000)
        bne.un.s        next8
        br                      fail
next8:
        ldc.r4          float32(0x7FC00000)
        ldc.r4          float32(0x7FC00000)
        bne.un.s        next9
        br                      fail
next9:
        ldc.r4          float32(0x7FC00000)
        ldc.r4          float32(0x3F800000)
        bne.un.s        next10
        br                      fail
next10:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x7FC00000)
        bne.un.s        pass
        br                      fail

pass:
        ldc.i4 100
        br                      end
fail:
        ldc.i4          0x0
        br                      end
end:
        ret
}


.assembly bne_s {}

