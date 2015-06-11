// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly b71869 { }
.class I {
.method static int32 Main() {
.entrypoint
.maxstack	9
.try {
    ldc.i4 0x6000000
    ldc.i4 0x6000000
  mul.ovf
conv.i4
leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}
}