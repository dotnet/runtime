// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.method public static int32 main(class [mscorlib]System.String[]) {
.locals         (class [mscorlib]System.String)
.entrypoint

        ldc.i4          0x44444444
        ldc.i4          0x55555555
        ble.s                   next0
        br                      fail
next0:
        ldc.i4          0x66666666
        ldc.i4          0x66666666
        ble.s                   next1
        br                      fail
next1:
        ldc.i4          0x55555555
        ldc.i4          0x44444444
        ble                   fail
        br                      next2
next2:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x3F800000)
        ble.s                   next3
        br                      fail
next3:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        ble.s                   next4
        br                      fail
next4:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0xBF800000)
        ble                   fail
        br                      next5
next5:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x3F800000)
        ble.un.s        next6
        br                      fail
next6:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0x3F800000)
        ble.un.s        next7
        br                      fail
next7:
        ldc.r4          float32(0x3F800000)
        ldc.r4          float32(0xBF800000)
        ble.un.s        fail
        br                      next8
next8:
        ldc.r4          float32(0x7FC00000)
        ldc.r4          float32(0x7FC00000)
        ble.un.s        next9
        br                      fail
next9:
        ldc.r4          float32(0x7FC00000)
        ldc.r4          float32(0x3F800000)
        ble.un.s        next10
        br                      fail
next10:
        ldc.r4          float32(0xBF800000)
        ldc.r4          float32(0x7FC00000)
        ble.un.s        pass
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


.assembly ble_s.exe {}

