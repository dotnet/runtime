// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd4820a77 {}
.assembly extern xunit.core {}
.class ILGEN_0xd4820a77 {

.field static	unsigned int64[] field_0x0
.field static	int64[] field_0x1
.field static	float32 field_0x2
.field static	unsigned int16[] field_0x3

.method static int32 Method_0x1a2ad58f(unsigned int8 Arg_0x0, unsigned int64 Arg_0x1, int8 Arg_0x2, float32 Arg_0x3) {
.maxstack	9

 .locals (unsigned int32 LOCAL_0x0,unsigned int16[] LOCAL_0x1,float32[] LOCAL_0x2)

ldc.i4 0x5f8d71d5 
stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x2

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xd4820a77::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xd4820a77::field_0x1
ldc.r4 float32(0xbda4a002) 
stsfld float32 ILGEN_0xd4820a77::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0xd4820a77::field_0x3


Start_Orphan_0:
Start_Orphan_1:
 ldc.i4.s -13
 brfalse Branch_0x0

 nop
 br Branch_0x1

 Branch_0x0:

 nop
 Branch_0x1:

End_Orphan_1:
 ldarga Arg_0x1
 pop
End_Orphan_0:
 ldarg Arg_0x3
 ldc.r4 float32(0x5bc47621)
 blt.un Branch_0x12

 ldc.i4.6
 br Branch_0x13

 Branch_0x12:

 ldarg Arg_0x0
 Branch_0x13:

 brtrue Branch_0x10

 ldc.i4.4
 conv.ovf.u2
 br Branch_0x11

 Branch_0x10:

 ldarg Arg_0x2
 ldloc LOCAL_0x0
 clt.un
 Branch_0x11:

 ldc.i4.s 43
 ldc.i4.7
 rem.un
 ldarg Arg_0x3
 ldsfld float32 ILGEN_0xd4820a77::field_0x2
 clt.un
 ble.un Branch_0x14

 ldc.i4.7
 ldc.i4.0
 mul.ovf
 br Branch_0x15

 Branch_0x14:

 ldarg Arg_0x3
 conv.i
 Branch_0x15:

 bgt.un Branch_0xc

 ldc.i4.2
 ldc.i4 0xd8c92830
 sub
 ldc.i4.m1
 ldc.i4.3
 blt.un Branch_0x18

 ldarg Arg_0x2
 br Branch_0x19

 Branch_0x18:

 ldc.i4.5
 Branch_0x19:

 blt Branch_0x16

 ldarg Arg_0x3
 ldarg Arg_0x3
 bne.un Branch_0x1a

 ldc.i4.s -116
 br Branch_0x1b

 Branch_0x1a:

 ldc.i4.4
 Branch_0x1b:

 br Branch_0x17

 Branch_0x16:

 ldarg Arg_0x1
 ldc.i8 0x1229678b8cf89a10
 blt Branch_0x1c

 ldc.i4.1
 br Branch_0x1d

 Branch_0x1c:

 ldloc LOCAL_0x0
 Branch_0x1d:

 Branch_0x17:

 br Branch_0xd

 Branch_0xc:

 ldc.i4.7
 ldarg Arg_0x0
 bgt.un Branch_0x20

 ldc.i4.s -30
 br Branch_0x21

 Branch_0x20:

 ldc.i4.s 4
 Branch_0x21:

 ldc.i4.4
 conv.ovf.u1
 blt.un Branch_0x1e

 ldc.i4.6
 ldc.i4.2
 cgt.un
 br Branch_0x1f

 Branch_0x1e:

 ldarga Arg_0x0
 ldind.u1
 Branch_0x1f:

 Branch_0xd:

Start_Orphan_d:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Int64
 ldarg Arg_0x0
 ldc.i4.3
 div.un
 ldc.i4.5
 brfalse Branch_0x22

 ldarg Arg_0x1
 br Branch_0x23

 Branch_0x22:

 ldarg Arg_0x1
 Branch_0x23:

 stelem.i8
End_Orphan_d:
 ldc.r8 float64(0x77d73968715c290d)
 ldc.r4 float32(0x7dfc1b3a)
 bgt Branch_0x26

 ldc.i4.7
 br Branch_0x27

 Branch_0x26:

 ldc.i4.2
 Branch_0x27:

 ldsfld float32 ILGEN_0xd4820a77::field_0x2
 conv.i1
 bgt Branch_0x24

 ldc.i4.0
 ldc.i4.2
 bge Branch_0x28

 ldc.i4.7
 br Branch_0x29

 Branch_0x28:

 ldc.i4.6
 Branch_0x29:

 br Branch_0x25

 Branch_0x24:

 ldc.i4.8
 ldc.i4.s -88
 mul.ovf.un
 Branch_0x25:

 conv.ovf.i4
 bge Branch_0x2

 ldarg Arg_0x1
 conv.ovf.i
 conv.ovf.u8.un
 ldc.i4.5
 conv.r4
 ldc.i8 0xe623a11cfd408f2d
 ldc.i8 0x19179f3caf47f2c4
 bne.un Branch_0x32

 ldc.r4 float32(0x3078912)
 br Branch_0x33

 Branch_0x32:

 ldarg Arg_0x3
 Branch_0x33:

 bgt.un Branch_0x30

 ldc.r4 float32(0x37f5bad6)
 ldc.r8 float64(0xc2d1c2dba20633b8)
 bge.un Branch_0x34

 ldarg Arg_0x1
 br Branch_0x35

 Branch_0x34:

 ldc.i8 0x9a66a198a5e43b9b
 Branch_0x35:

 br Branch_0x31

 Branch_0x30:

 ldc.i4.7
 ldc.i4 0x77d9bf23
 bgt.un Branch_0x36

 ldarg Arg_0x1
 br Branch_0x37

 Branch_0x36:

 ldc.i8 0xcaf52ff2f727ccb6
 Branch_0x37:

 Branch_0x31:

 ble Branch_0x2a

 ldloc LOCAL_0x0
 br Branch_0x2b

 Branch_0x2a:

 ldc.r8 float64(0xd61b110c1818c7ad)
 ldarg Arg_0x3
 clt.un
 ldc.i8 0x9db61626c7ddb1db
 ldc.i8 0x3514eed7ad681554
 beq Branch_0x38

 ldc.i4.0
 br Branch_0x39

 Branch_0x38:

 ldc.i4.1
 Branch_0x39:

 mul
 Branch_0x2b:

 br Branch_0x3

 Branch_0x2:

 ldarg Arg_0x1
 conv.ovf.i
 conv.ovf.i2
 Branch_0x3:

EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0xd9b121d4 
ldc.i8 0x2ff46571d8f3878e 
ldc.i4 0x6b12e8f4 
ldc.r4 float32(0x127a6c66) 

call int32 ILGEN_0xd4820a77::Method_0x1a2ad58f(unsigned int8 Arg_0x0, unsigned int64 Arg_0x1, int8 Arg_0x2, float32 Arg_0x3)
 pop
leave END
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
