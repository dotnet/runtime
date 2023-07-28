// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b20217' {}
.class public ILGEN_0x38eb1044 {

.field static	unsigned int8 field_0x0

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	6

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x8
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
ldc.i4 0x201d4f22 
stloc local_0xa
ldc.i4 0x542110bc 
stloc local_0xb
ldc.i4 0x65e44a5a 
stloc local_0xc
ldc.i4 0x22b81a57 
stloc local_0xd
ldc.i4 0x51612798 
stloc local_0xe
ldc.i8 0x1dd0280e14d63376 
stloc local_0xf
ldc.r4 float32(0x6a336df6) 
stloc local_0x10
ldc.r8 float64(0x458773098506ca2) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x2ae57241 
stsfld unsigned int8 ILGEN_0x38eb1044::field_0x0

 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Int64
 ldloca local_0xc
 ldind.i1
 ldloc local_0xf
 ldloc local_0xf
 add
 stelem.i8
End_Orphan_0:
Start_Orphan_2:
 ldloc local_0x9
 stloc local_0x9
End_Orphan_2:

 ldloc local_0x8
 ldlen
 
 pop
 leave xx
 } catch [mscorlib]System.IndexOutOfRangeException {
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
