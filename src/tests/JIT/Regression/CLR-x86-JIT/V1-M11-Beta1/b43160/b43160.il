// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x19b572e2 {}
.assembly extern xunit.core {}
.class ILGEN_0x19b572e2 {

.field static	int16 field_0x0
.field static	int64[] field_0x1
.field static	int32 field_0x2

.method static int32 Method_0x1a700c9c(float64 Arg_0x0, int8 Arg_0x1) {
.maxstack	13
.locals (unsigned int8 local_0x0,unsigned int64 local_0x1,float32 local_0x2,int64 local_0x4,int16[] local_0x5)

ldc.i4 0xafa2e24e 
stloc local_0x0
ldc.i8 0x4d9b00b7f4635cea 
stloc local_0x1
ldc.r4 float32(0xc85ad4f0) 
stloc local_0x2
ldc.i8 0xdaaf6cf9801304db 
stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5

ldc.i4 0x6ba0cb2b 
stsfld int16 ILGEN_0x19b572e2::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x19b572e2::field_0x1
ldc.i4 0xfc873c17 
stsfld int32 ILGEN_0x19b572e2::field_0x2

Start_Orphan_0:
 ldc.i4.5
 ldc.i4.7
 bge Branch_0x2

 ldc.i8 0x3ab42cf9b7302bfa
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0xd583dea3ae5aeb07
 Branch_0x3:

 ldc.i4.0
 ldc.i4 0x267d990f
 ble.un Branch_0x4

 ldc.i8 0x468cabb1bfa6593
 br Branch_0x5

 Branch_0x4:

 ldc.i8 0x6b7f6de5724f2fb
 Branch_0x5:

 add
Start_Orphan_2:
 ldloc local_0x5
 ldc.i4.3
 ldarg Arg_0x1
 stelem.i2
End_Orphan_2:
 ldc.i8 0x244bf2d4d99ffc07
 ldloc local_0x1
 mul.ovf.un
 ldloc local_0x2
 ldc.r4 float32(0x20ede078)
 bge.un Branch_0x6

 ldloc local_0x4
 br Branch_0x7

 Branch_0x6:

 ldc.i8 0xf408c5dc51631d0c
 Branch_0x7:

 or
 blt.un Branch_0x0

 ldc.i4.5
 ldc.i4.4
 blt Branch_0x8

 ldloc local_0x1
 br Branch_0x9

 Branch_0x8:

 ldloc local_0x4
 Branch_0x9:

 stloc local_0x4
 br Branch_0x1

 Branch_0x0:

 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.Int64
 stsfld int64[] ILGEN_0x19b572e2::field_0x1
 Branch_0x1:

End_Orphan_0:
 ldc.i4.4
 conv.ovf.i1
 ldc.i4.5
 ldsfld int32 ILGEN_0x19b572e2::field_0x2
 rem
 conv.i4
Start_Orphan_b:
 ldloca local_0x0
 ldc.i4.0
 stind.i1
End_Orphan_b:
 conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_d:
 ldsfld int16 ILGEN_0x19b572e2::field_0x0
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i4.8
 ldelema [mscorlib]System.Int64
Start_Orphan_10:
 ldloca local_0x4
 ldloc local_0x1
 stind.i8
End_Orphan_10:
 ldc.r8 float64(0xc4ac103073e349b8)
 conv.i8
 stind.i8
End_Orphan_d:
 ldloc local_0x4
 conv.ovf.i4.un
 ldelem.i1
 cgt.un
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
	ldc.r8 float64(0x31b660a866eb1ae9) 
	ldc.i4 0xa5ff46dc 
	call int32 ILGEN_0x19b572e2::Method_0x1a700c9c(float64 Arg_0x0, int8 Arg_0x1)
	pop
	leave out_of_here
} catch [mscorlib]System.OverflowException { 
	pop
	leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
