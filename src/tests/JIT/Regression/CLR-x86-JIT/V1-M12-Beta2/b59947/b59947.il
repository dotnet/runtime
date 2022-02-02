// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b59947' {}
.assembly extern xunit.core {}
.class ILGEN_0xf2c69f5c {

.field static	unsigned int16[] field_0x1
.field static	unsigned int8 field_0x2

.method static int32 Method_0xd9a52b3d(int8 Arg_0x0) {
.maxstack	9

 .locals (unsigned int32 LOCAL_0x0)

ldc.i4 0xfb96252c 
stloc LOCAL_0x0

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld unsigned int16[] ILGEN_0xf2c69f5c::field_0x1
ldc.i4 0xe66c615 
stsfld unsigned int8 ILGEN_0xf2c69f5c::field_0x2

Start_Orphan_0:
 ldc.r8 float64(0x75149991efeb61c)
 conv.u2
 ldc.i8 0x328202fcdfa3343
 conv.i
 bgt.un Branch_0x2

 ldsfld unsigned int8 ILGEN_0xf2c69f5c::field_0x2
 ldc.i4.0
 beq Branch_0x4

 ldc.r4 float32(0x4be0f020)
 br Branch_0x5

 Branch_0x4:

 ldc.r8 float64(0x6d1e0ea6bff35aae)
 Branch_0x5:

 br Branch_0x3

 Branch_0x2:

 ldc.r8 float64(0x4c6d818ad80bd162)
 Branch_0x3:

 ldc.i8 0xd9053d7652362021
 conv.r.un
 bge Branch_0x0

 br Branch_0x1

 Branch_0x0:

 ldc.i4.0
 ldc.i4.7
 ldloc LOCAL_0x0
 add
 blt.un Branch_0x10

 ldsfld unsigned int16[] ILGEN_0xf2c69f5c::field_0x1
 ldc.i4.m1
 ldelema [mscorlib]System.Int16
 ldc.i4.7
 stind.i2
 br Branch_0x11

 Branch_0x10:

 Branch_0x11:

 Branch_0x1:

End_Orphan_0:
 ldc.i4.2
 ldc.i4.7
 ceq
 ldc.i4.7
 ldc.i4.s -91
 blt.un Branch_0x24

 ldc.i8 0x6b2710de7d055753
 br Branch_0x25

 Branch_0x24:

 ldc.i8 0x87775c805359bc53
 Branch_0x25:

 conv.r4
 ldarg Arg_0x0
 ldc.i4.0
 ldc.i4 0xaa23a8f7
 bge.un Branch_0x28

 ldc.i4.8
 br Branch_0x29

 Branch_0x28:

 ldc.i4.6
 Branch_0x29:

pop
pop
ldc.r4 float32(0xeaeed99b)

 Branch_0x27:

 cgt.un
 div.un
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i4 0xc4d19849 
call int32 ILGEN_0xf2c69f5c::Method_0xd9a52b3d(int8 Arg_0x0)
ldc.i4 100
add
ret

}

}
