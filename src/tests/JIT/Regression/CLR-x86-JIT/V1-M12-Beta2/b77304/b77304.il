// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b77304 { }
.class ILGEN_0x65088b5c {

//BEGIN STATIC FIELDS
.field static	int64[] field_0x0
.field static	unsigned int32[] field_0x1
.field static	native int field_0x2
.field static	int16[] field_0x3
.field static	int16[] field_0x4
.field static	int32[] field_0x5
.field static	float32 field_0x6
.field static	unsigned int32[] field_0x7
.field static	float32 field_0x8
//END STATIC FIELDS
.method static int64  Method_0x5ad2583a(int64 Arg_0x0, unsigned int16 Arg_0x1, int32 Arg_0x2, unsigned int8 Arg_0x3, int8 Arg_0x4, int16 Arg_0x5, unsigned int8 Arg_0x6) {
.maxstack	19

 .locals (int32 LOCAL_0x0,object LOCAL_0x1,int32 LOCAL_0x2,unsigned int8 LOCAL_0x3,int8[] LOCAL_0x4,int64[] LOCAL_0x5,float32[] LOCAL_0x6)
//BEGIN INIT LOCALS
ldc.i4 0xc4964c2c 
stloc LOCAL_0x0
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x1
ldc.i4 0xe3fea0f 
stloc LOCAL_0x2
ldc.i4 0x618b0f04 
stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x6

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x65088b5c::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x65088b5c::field_0x1
ldc.i4 0x13aad7ea 
stsfld native int ILGEN_0x65088b5c::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x65088b5c::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x65088b5c::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x65088b5c::field_0x5
ldc.r4 float32(0xeb3a3848) 
stsfld float32 ILGEN_0x65088b5c::field_0x6
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x65088b5c::field_0x7
ldc.r4 float32(0x7b510480) 
stsfld float32 ILGEN_0x65088b5c::field_0x8

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
       ldsfld native int ILGEN_0x65088b5c::field_0x2
       ldc.i4.5
       ldc.i4.2
 beq Branch_0xc

       ldsfld native int ILGEN_0x65088b5c::field_0x2
 br Branch_0xd

 Branch_0xc:

       ldsfld native int ILGEN_0x65088b5c::field_0x2
 Branch_0xd:

       ldc.i8 0xc99fc9f64e2a2ab3
       ldc.i8 0x68f478c75a1cca35
 blt.un Branch_0xe

       ldsfld native int ILGEN_0x65088b5c::field_0x2
 br Branch_0xf

 Branch_0xe:

       ldsfld native int ILGEN_0x65088b5c::field_0x2
 Branch_0xf:

 bgt.un Branch_0xa

       ldsfld native int ILGEN_0x65088b5c::field_0x2
      conv.ovf.i1.un
 br Branch_0xb

 Branch_0xa:

       ldloc LOCAL_0x3
      conv.ovf.i2.un
 Branch_0xb:

    mul
       ldsfld native int ILGEN_0x65088b5c::field_0x2
      conv.ovf.u2.un
 ble.un Branch_0x6

 br Branch_0x7

 Branch_0x6:

     ldloc LOCAL_0x6
    pop
 Branch_0x7:

End_Orphan_1:
        ldsfld native int ILGEN_0x65088b5c::field_0x2
      conv.u1
     conv.u1
 newarr [mscorlib]System.Int64
        ldc.r8 float64(0x2c84c0b84961cd37)
        ldsfld float32 ILGEN_0x65088b5c::field_0x8
       ceq
      conv.ovf.i4.un
    ldelema [mscorlib]System.Int64
   ldind.i8
      ldarga Arg_0x0
     ldind.i8
    conv.ovf.i8.un
 bgt.un Branch_0x4

     ldc.i4.4
 br Branch_0x5

 Branch_0x4:

        ldarg Arg_0x2
        ldsfld native int ILGEN_0x65088b5c::field_0x2
      sub
        ldc.i8 0xe15d2263cf418e41
       conv.ovf.i1.un
     add
Start_Orphan_2e:
       ldc.i4.3
      starg Arg_0x6
End_Orphan_2e:
       ldloc LOCAL_0x5
        ldc.i8 0x1ad8ed69486ff20b
       conv.ovf.i1
      ldelem.i8
        ldc.i8 0x61a119e38630ac12
     cgt
    sub.ovf.un
 Branch_0x5:

        ldc.i4 0x82c7a587
       conv.ovf.u8.un
     conv.ovf.i1.un
        ldarg Arg_0x1
      conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_5c:
       ldsfld int32[] ILGEN_0x65088b5c::field_0x5
       ldarg Arg_0x1
       ldarg Arg_0x1
      stelem.i4
End_Orphan_5c:
        ldsfld native int ILGEN_0x65088b5c::field_0x2
       conv.ovf.u4
     ldelem.r4
    conv.ovf.i8
   conv.ovf.u
  add.ovf.un
 bge.un Branch_0x2

 br Branch_0x3

 Branch_0x2:

      ldarga Arg_0x6
     ldind.u1
 newarr [mscorlib]System.SByte
  pop
 Branch_0x3:

End_Orphan_0:
ldc.i8 44
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	15
.try {
ldc.i8 0x659c38a2d445bbf9 
ldc.i4 0xaf86507f 
ldc.i4 0x6244cc06 
ldc.i4 0x4bc2fa5e 
ldc.i4 0xdcb90886 
ldc.i4 0x52215587 
ldc.i4 0x87913e2f 

 call int64  ILGEN_0x65088b5c::Method_0x5ad2583a(int64 Arg_0x0, unsigned int16 Arg_0x1, int32 Arg_0x2, unsigned int8 Arg_0x3, int8 Arg_0x4, int16 Arg_0x5, unsigned int8 Arg_0x6)
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
