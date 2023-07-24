// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

	.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly 'b25463' {}
.class public ILGEN_0x7122ba20 {

.field static	unsigned int64 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11, int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {
 
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 0x182a6a99 
stloc local_0xa
ldc.i4 0x5e8c6ce 
stloc local_0xb
ldc.i4 0x2c94f3e 
stloc local_0xc
ldc.i4 0x106dbe 
stloc local_0xd
ldc.i4 0x75894acf 
stloc local_0xe
ldc.i8 0xb5a128be8a652b 
stloc local_0xf
ldc.r4 float32(0x64f642ea) 
stloc local_0x10
ldc.r8 float64(0x7b6b6e9714c4588e) 
stloc local_0x11

ldc.i8 0x69ee490192b41eb 
stsfld unsigned int64 ILGEN_0x7122ba20::field_0x3

 ldsfld unsigned int64 ILGEN_0x7122ba20::field_0x3
 conv.ovf.i8
//Start_Orphan_2:
 ldc.i4 0x32f614ee
 stloc local_0xc
//End_Orphan_2:
 ldloc local_0xf
 ldloc local_0xc

 
 shr
 
 ldsfld unsigned int64 ILGEN_0x7122ba20::field_0x3
 
 mul.ovf
 
 sub.ovf.un

//Start_Orphan_4:
 ldloc local_0x7
 ldc.i4.6
 ldc.i8 0x8ddf2d18d12596
 stelem.i8
//End_Orphan_4:

 ldloc local_0x3
 ldc.i4.4
 ldelem.u8

//Start_Orphan_5:
 ldc.i4.0
 stloc local_0xd
//End_Orphan_5:

 rem.un
 conv.ovf.i4
 
 
 pop
 leave xx
 } catch [mscorlib]System.Exception {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}
