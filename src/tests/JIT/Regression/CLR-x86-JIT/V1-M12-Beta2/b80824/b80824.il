// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b80824' {}
.assembly extern xunit.core {}
.class ILGEN_0x8485715d {

//BEGIN STATIC FIELDS
//END STATIC FIELDS
.method static unsigned int8  Method_0xda91553f(native int Arg_0x0, int32 Arg_0x1, unsigned int64 Arg_0x2, float64 Arg_0x3, unsigned int8 Arg_0x4, int8 Arg_0x5, int32 Arg_0x6, int32 Arg_0x7, int64 Arg_0x8, float32 Arg_0x9, unsigned int32 Arg_0xa, unsigned int64 Arg_0xb, float32 Arg_0xc) {
.maxstack	11

 .locals ()
//BEGIN INIT LOCALS

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
  nop
End_Orphan_2:
  ldc.i8 0xa4f61774a793e8b2
 starg Arg_0xb
End_Orphan_1:
Start_Orphan_3:
  nop
End_Orphan_3:
Start_Orphan_4:
   nop
End_Orphan_4:
    ldc.i4.s 121
   conv.u8
  conv.r4
 call float64 [mscorlib]System.Math::Round(float64)
starg Arg_0x9
End_Orphan_0:
   ldarga Arg_0x3
  ldind.r8
 call float64 [mscorlib]System.Math::Abs(float64)
conv.ovf.u2
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	27
ldc.i4 0x37232c8a conv.i
ldc.i4 0x2b30fc6c 
ldc.i8 0xf6287727f37f8cb6 
ldc.r8 float64(0xa696bb6a3fb9d52c) 
ldc.i4 0x861cdd9d 
ldc.i4 0x27c3439 
ldc.i4 0x3c3110f6 
ldc.i4 0x138a47c7 
ldc.i8 0xef15efcae6e6494b 
ldc.r4 float32(0xa5e12e46) 
ldc.i4 0x3435fd8f 
ldc.i8 0x5cf608a5646dcedf 
ldc.r4 float32(0xdfa1608f) 

 call unsigned int8  ILGEN_0x8485715d::Method_0xda91553f(native int Arg_0x0, int32 Arg_0x1, unsigned int64 Arg_0x2, float64 Arg_0x3, unsigned int8 Arg_0x4, int8 Arg_0x5, int32 Arg_0x6, int32 Arg_0x7, int64 Arg_0x8, float32 Arg_0x9, unsigned int32 Arg_0xa, unsigned int64 Arg_0xb, float32 Arg_0xc)
conv.i4
ldc.i4.s 100
add
 ret
}

}
