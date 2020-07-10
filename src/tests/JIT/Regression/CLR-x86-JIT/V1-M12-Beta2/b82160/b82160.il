// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb45a7d8a {}
.class ILGEN_0xb45a7d8a {
.field static	native int field_0x0
.field static	unsigned int64 field_0x1
.field static	int64 field_0x4

.method static int32  Method_0xd47d5ddc() {
.maxstack	15

 .locals (float32 LOCAL_0x4,int32[] LOCAL_0x5,float32 LOCAL_0x6,unsigned int32[] LOCAL_0x7)
ldc.r4 float32(0xd537f67e) 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x5
ldc.r4 float32(0x5712f446) 
stloc LOCAL_0x6
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x7


ldc.i4 0xaa757771 
stsfld native int ILGEN_0xb45a7d8a::field_0x0
ldc.i8 0x95f048913fb5cc65 
stsfld unsigned int64 ILGEN_0xb45a7d8a::field_0x1
ldc.i8 0x2b5c4b372c6d40f1 
stsfld int64 ILGEN_0xb45a7d8a::field_0x4

Start_Orphan_0:
      ldsfld int64 ILGEN_0xb45a7d8a::field_0x4
     conv.i8
    conv.ovf.u4.un
      ldsflda unsigned int64 ILGEN_0xb45a7d8a::field_0x1
     ldind.u8
      ldsfld unsigned int64 ILGEN_0xb45a7d8a::field_0x1
      ldsfld int64 ILGEN_0xb45a7d8a::field_0x4
     add.ovf.un
    cgt
   rem.un
  conv.u1
 newarr [mscorlib]System.Int64
   ldloc LOCAL_0x4
  conv.i4
 ldelema [mscorlib]System.Int64
pop
End_Orphan_0:
  ldc.r8 float64(0x78dc06c2e4540dc8)
Start_Orphan_8:
      ldsfld int64 ILGEN_0xb45a7d8a::field_0x4
     conv.ovf.i4.un
    stsfld native int ILGEN_0xb45a7d8a::field_0x0
End_Orphan_8:
       ldc.i4.4
     conv.r.un
    conv.ovf.i8
   conv.i8
Start_Orphan_b:
      ldloc LOCAL_0x5
      ldc.i4.3
     ldelema [mscorlib]System.Int32
      ldc.i8 0x273043b34e2203a7
     conv.ovf.u4.un
    stind.i
End_Orphan_b:
 conv.r8
cgt.un
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	1
.try {
  call int32  ILGEN_0xb45a7d8a::Method_0xd47d5ddc()
  leave.s IL_BAD
} catch [mscorlib]System.OverflowException {
  leave.s IL_GOOD
}
IL_GOOD:
ldc.i4.s 100
 ret
IL_BAD:
ldc.i4.s 101
 ret
}

}

