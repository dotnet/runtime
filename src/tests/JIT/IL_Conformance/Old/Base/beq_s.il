// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.method public static int32 main(class [mscorlib]System.String[]) {
.locals         (class [mscorlib]System.String)
.entrypoint

        ldc.i4          0xFFAAFFAA
        ldc.i4          0xFFAAFFAA
        beq.s                   next0
        br                      fail
next0:
        ldc.i4          0xDDDD0000
        ldc.i4          0x0000DDDD
        beq.s                   fail
        br                      next1
next1:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        beq.s                   next2
        br                      fail
next2:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x7F800000)
        beq.s                   fail
        br                      next3

next3:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        beq.s                   next4
        br                      fail
next4:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0xBF800000)
        beq.s                   fail
        br                      next5
next5:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x3F800000)
        beq.s                   fail
        br                      pass

pass:
        ldc.i4 100
        br                      end
fail:
        ldc.i4          0x0
        br                      end
end:
        ret
}


.assembly beq_s {}

