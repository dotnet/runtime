// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x44f8f663 {}
.class ILGEN_0x44f8f663 {
.method static int32 Method_0x2e75f3b4(int8 Arg_0x0, unsigned int64 Arg_0x1, int16 Arg_0x2) {
.maxstack	15
Start_Orphan_0:
 ldc.i4.2
 ldc.i4.0
 mul
 conv.u
 brfalse Branch_0x0

 ldc.i4.6
 conv.r4
 conv.r8
 ckfinite
 ckfinite
 ldc.r8 float64(0x20d0f36a9cbd3840)
 bgt.un Branch_0x4

 ldc.i8 0x58e1e98668ca36e
 conv.r8
 conv.u1
 starg Arg_0x2
 br Branch_0x5

 Branch_0x4:

 ldc.r8 float64(0x4a1481fcba49a876)
 conv.u
 conv.i2
 conv.ovf.i8.un
 starg Arg_0x1
 Branch_0x5:

 br Branch_0x1

 Branch_0x0:

 ldc.i8 0xcb951bc1de3beecf
 conv.ovf.i
 ldc.i4.5
 ldarg Arg_0x0
 xor
 add
 conv.ovf.u4
 conv.ovf.i.un
 pop
 Branch_0x1:

End_Orphan_0:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i8 0xf06da55ebb83d68e
Start_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4.s -40
 conv.ovf.u
 conv.ovf.u1.un
 starg Arg_0x2
End_Orphan_12:
Start_Orphan_14:
 nop
End_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.r8 float64(0x4d95800d5c7f3082)
 conv.u
 conv.r4
 conv.u8
 mul
Start_Orphan_16:
 ldc.r4 float32(0x5404c807)
 ldc.r8 float64(0xc984d38211a7f0f3)
 clt.un
 ldarg Arg_0x1
 conv.u2
 ceq
Start_Orphan_17:
 nop
End_Orphan_17:
 ldc.i4.1
 conv.ovf.u2
 conv.ovf.u2.un
 or
 starg Arg_0x0
End_Orphan_16:
Start_Orphan_18:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldc.i4.3
 ldc.i4.s 3
 shr
 conv.i8
 starg Arg_0x1
End_Orphan_18:
Start_Orphan_1a:
 ldarg Arg_0x0
 pop
End_Orphan_1a:
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldc.i8 0x18f8d81396e5fd2
 ldc.i8 0x2211b6759b180c91
 or
 ldc.i8 0xdd8098a43c05c79
 ldc.i8 0xa520d42b3831c231
 rem.un
 rem.un
 conv.ovf.i8
 ldc.i4 0x58aa33bd
 ldc.i4.4
 add.ovf
 conv.ovf.u8
 ldc.i4.4
 ldc.i4.4
 clt.un
 conv.ovf.i8
 or
 mul
Start_Orphan_1e:
 ldarg Arg_0x1
 ldarg Arg_0x1
 add.ovf.un
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldarg Arg_0x1
 ldarg Arg_0x1
 xor
 or
 starg Arg_0x1
End_Orphan_1e:
Start_Orphan_20:
 nop
End_Orphan_20:
 ldarg Arg_0x1
 neg
 not
Start_Orphan_21:
 ldc.r8 float64(0xc0d5aca6af77885d)
 conv.ovf.i4
 starg Arg_0x2
End_Orphan_21:
Start_Orphan_23:
 ldc.i4.0
 starg Arg_0x0
End_Orphan_23:
 ldc.i8 0x212c9b48bf87f0c9
 conv.i8
 ldc.i4.2
 conv.ovf.u2.un
 conv.ovf.u8.un
 div
 or
 sub.ovf
 add.ovf
 conv.ovf.i4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x8754f793 
ldc.i8 0xed2e9c5c0d3dce680 
ldc.i4 0x1fdf1aec 

call int32 ILGEN_0x44f8f663::Method_0x2e75f3b4(int8 Arg_0x0, unsigned int64 Arg_0x1, int16 Arg_0x2)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
