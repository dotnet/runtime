// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xdd840404 {}
.class ILGEN_0xdd840404 {

.field static	unsigned int32 field_0x0

.method static int32 Method_0x6dae498c(unsigned int64 Arg_0x0, unsigned int8 Arg_0x1, unsigned int32 Arg_0x2, int32 Arg_0x3) {
.maxstack	9

 .locals (int32 LOCAL_0x0,unsigned int64 LOCAL_0x1)

ldc.i4 0x32a9150b 
stloc LOCAL_0x0
ldc.i8 0x3dbe6f10fae37672 
stloc LOCAL_0x1

ldc.i4 0xa806b390 
stsfld unsigned int32 ILGEN_0xdd840404::field_0x0

Start_Orphan_0:
 ldsfld unsigned int32 ILGEN_0xdd840404::field_0x0
 ldc.i8 0x9ed5a3e5164e7a32
 ldarg Arg_0x0
 bne.un Branch_0x2

 ldc.i4.5
 br Branch_0x3

 Branch_0x2:

 ldarg Arg_0x2
 Branch_0x3:

 conv.ovf.i2.un
 bge.un Branch_0x0

Start_Orphan_5:
 nop
End_Orphan_5:
 ldarg Arg_0x3
 ldc.i4.1
 bne.un Branch_0x6

 ldarg Arg_0x1
 br Branch_0x7

 Branch_0x6:

 ldc.i4.3
 Branch_0x7:

 ldc.i4.3
 blt Branch_0x4

 br Branch_0x5

 Branch_0x4:

 Branch_0x5:

 br Branch_0x1

 Branch_0x0:

 ldloc LOCAL_0x1
 not
 ldc.r8 float64(0x905192e1e28b44e0)
 conv.i8
 blt.un Branch_0xe

 br Branch_0xf

 Branch_0xe:

 ldarga Arg_0x0
 ldloc LOCAL_0x1
 stind.i8
 Branch_0xf:

 Branch_0x1:

End_Orphan_0:
Start_Orphan_6:
 ldloca LOCAL_0x0
 ldind.i4
 stsfld unsigned int32 ILGEN_0xdd840404::field_0x0
End_Orphan_6:
 ldc.r8 float64(0xfb4e337ef6ee1b2)
 ldc.r8 float64(0xd605560a7f690745)
 ldc.r8 float64(0xd35a681fe471165a)
 ble Branch_0x16

 ldc.i4.4
 br Branch_0x17

 Branch_0x16:

 ldc.i4.2
 Branch_0x17:

 ldc.i4.4
 conv.ovf.u4.un
 beq Branch_0x14

 ldc.r8 float64(0xd8fecb9fc9591255)
 br Branch_0x15

 Branch_0x14:
 ldc.r4 float32(0x8b9ec3c4)
 
 Branch_0x15:

 ceq
 conv.i1
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0xcd63efdcf3f05311 
ldc.i4 0xdab66cb5 
ldc.i4 0x40a8681 
ldc.i4 0xa71f86bd 

call int32 ILGEN_0xdd840404::Method_0x6dae498c(unsigned int64 Arg_0x0, unsigned int8 Arg_0x1, unsigned int32 Arg_0x2, int32 Arg_0x3)
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
