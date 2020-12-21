// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x493230e7 {}
.class ILGEN_0x493230e7 {
.method static int32 Method_0x2d7c921() {
.maxstack	11
 ldc.r4 float32(0x6ef1549c)
 call float64 [mscorlib]System.Math::Sin(float64)
 ldc.r8 4.4
 bgt Branch_0x2
 ldc.i4 0xe
 br Branch_0x3
Branch_0x2:
 ldc.i4.6
Branch_0x3:
 ldc.i4.8
 ble.un Branch_0x0
 br Branch_0x1
Branch_0x0:
 ldc.i4.8
 ldc.i4.3
 ldc.i4 0xc
 add
 sub.ovf.un
 pop
Branch_0x1:
 ldc.i4 0x8e9bf46e
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x493230e7::Method_0x2d7c921()
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}
}
