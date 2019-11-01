// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b71831 { }
.class ILGEN_0x42acd912 {

//BEGIN STATIC FIELDS
.field static	unsigned int16 field_0x0
.field static	int8 field_0x1
.field static	unsigned int64[] field_0x2
.field static	float64 field_0x3
.field static	float64 field_0x4
.field static	float64[] field_0x5
.field static	unsigned int16[] field_0x6
//END STATIC FIELDS
.method static int32  Method_0xc7da8033(unsigned int32 Arg_0x0) {
.maxstack	11

 .locals (int32[] LOCAL_0x0,object LOCAL_0x1,float32[] LOCAL_0x2,int8 LOCAL_0x3)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x0
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x2
ldc.i4 0x8d03155a 
stloc LOCAL_0x3

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0xfcf3a69d 
stsfld unsigned int16 ILGEN_0x42acd912::field_0x0
ldc.i4 0x39c0ec84 
stsfld int8 ILGEN_0x42acd912::field_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x42acd912::field_0x2
ldc.r8 float64(0x570dbe2d6adea447) 
stsfld float64 ILGEN_0x42acd912::field_0x3
ldc.r8 float64(0xdc9e1a91d97996c) 
stsfld float64 ILGEN_0x42acd912::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x42acd912::field_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x42acd912::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
    ldc.i8 0xf6395900619bb8fe
/*c4*/
/*a4*/
/*b4*/
    ldc.i8 0x6ac6c619151d3eeb
/*c4*/
/*b3*/
   cgt
/*c3*/
/*b2*/
  conv.u1
 newarr [mscorlib]System.Single
/*c2*/
/*a2*/
/*a3*/
Start_Orphan_5:
/*a3*/
/*b3*/
   nop
/*c3*/
End_Orphan_5:
/*a4*/
/*b4*/
    ldc.i4.7
/*c4*/
/*a4*/
/*b4*/
    ldc.i4.5
/*c4*/
/*b3*/
   and
/*c3*/
/*a3*/
Start_Orphan_6:
/*a3*/
/*b3*/
   nop
/*c3*/
End_Orphan_6:
/*a4*/
/*b4*/
    ldc.r8 float64(0xabad5ff5349f6af6)
/*c4*/
/*a4*/
/*b4*/
    ldc.r8 float64(0x8fb2a640af68010d)
/*c4*/
/*b3*/
   cgt.un
/*c3*/
/*b2*/
  sub.ovf
/*c2*/
/*b1*/
 ldelema [mscorlib]System.Single
/*c1*/
/*b0*/
pop
/*c0*/
End_Orphan_0:
/*a1*/
Start_Orphan_7:
/*a1*/
/*a2*/
/*a3*/
/*a4*/
/*b4*/
    ldc.i4.5
/*c4*/
/*a4*/
/*b4*/
    ldc.i4.2
/*c4*/
/*b3*/
   mul
/*c3*/
/*b2*/
  conv.ovf.u4
/*c2*/
/*b1*/
 stloc LOCAL_0x3
/*c1*/
End_Orphan_7:
/*b1*/
 ldloc LOCAL_0x0
/*c1*/
/*a1*/
Start_Orphan_8:
/*a1*/
/*a2*/
Start_Orphan_9:
/*a2*/
/*b2*/
  nop
/*c2*/
End_Orphan_9:
/*a3*/
/*a4*/
/*b4*/
    ldc.r4 float32(0xbc20279f)
/*c4*/
/*b3*/
   conv.ovf.u4
/*c3*/
/*b2*/
  conv.u1
 newarr [mscorlib]System.UInt64
/*c2*/
/*a2*/
Start_Orphan_a:
/*a2*/
/*a3*/
/*b3*/
   ldsfld int8 ILGEN_0x42acd912::field_0x1
/*c3*/
/*b2*/
  starg Arg_0x0
/*c2*/
End_Orphan_a:
/*a3*/
/*a4*/
/*b4*/
    ldc.i8 0x5da9d5047c166a54
/*c4*/
/*b3*/
   conv.ovf.u1
/*c3*/
/*b2*/
  conv.u1
/*c2*/
/*a2*/
/*a3*/
/*a4*/
/*b4*/
    ldc.i4.m1
/*c4*/
/*a4*/
/*b4*/
    ldc.i4.0
/*c4*/
/*b3*/
   add.ovf
/*c3*/
/*b2*/
  conv.u8
/*c2*/
/*b1*/
 stelem.i8
/*c1*/
End_Orphan_8:
/*a2*/
/*b2*/
  ldarga Arg_0x0
/*c2*/
/*b1*/
 ldind.u4
/*c1*/
/*b0*/
ldelem.i4
/*c0*/
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	3
.try {
ldc.i4 0xb1892ee9 

 call int32  ILGEN_0x42acd912::Method_0xc7da8033(unsigned int32 Arg_0x0)
conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.IndexOutOfRangeException {
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
