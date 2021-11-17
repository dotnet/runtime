// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b73786 { }
.assembly extern xunit.core {}
.class ILGEN_0x4a792db4 {
.field static	unsigned int64 field_0x0
.method static int32  Method_0x2a166130(int64 Arg_0x0, unsigned int16 Arg_0x1, unsigned int8 Arg_0x2, float64 Arg_0x3, unsigned int8 Arg_0x4, unsigned int32 Arg_0x5, int64 Arg_0x6) {
.maxstack	15

 .locals (float32[] LOCAL_0x0,unsigned int32[] LOCAL_0x1,native int[] LOCAL_0x2,float32[] LOCAL_0x3,float32[] LOCAL_0x4,native int LOCAL_0x5,int64[] LOCAL_0x6)
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x4
ldc.i4 0x446170df 
stloc LOCAL_0x5

ldc.i8 0x8299df9c66697164 
stsfld unsigned int64 ILGEN_0x4a792db4::field_0x0

Start_Orphan_1:
   ldloc LOCAL_0x3
    ldc.i8 0xe0a280e1b5adb8c
   conv.u2
  ldelema [mscorlib]System.Single
     ldloc LOCAL_0x5
   conv.ovf.i1
  conv.r4
 stind.r4
End_Orphan_1:
  ldloc LOCAL_0x5
  conv.ovf.u2
	Start_Orphan_2d:
    ldarg Arg_0x6
      ldsfld unsigned int64 ILGEN_0x4a792db4::field_0x0
     conv.ovf.u2.un
    conv.ovf.u8.un
   rem
  pop
End_Orphan_2d:
 Branch_0x3:

 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	15
.try {
ldc.i8 0x57e96eb2fd567464
ldc.i4 0xe2e11689
ldc.i4 0xba60815d
ldc.r8 float64(0xbf4e6f05562a8693)
ldc.i4 0x153b6b6f
ldc.i4 0x5cb05396
ldc.i8 0x789b27fdb00aed6d 

 call int32  ILGEN_0x4a792db4::Method_0x2a166130(int64 Arg_0x0, unsigned int16 Arg_0x1, unsigned int8 Arg_0x2, float64 Arg_0x3, unsigned int8 Arg_0x4, unsigned int32 Arg_0x5, int64 Arg_0x6)
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
