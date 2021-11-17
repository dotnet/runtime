// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1b57e965 {}
.assembly extern xunit.core {}
.class ILGEN_0x1b57e965 {
.field static	int8[] field_0x0
.method static float64  Method_0xe5b0e4a3(float64 Arg_0x0) {
.maxstack	15
.locals (int8[] LOCAL_0x0,object LOCAL_0x1,float32 LOCAL_0x2,unsigned int16 LOCAL_0x3,unsigned int32 LOCAL_0x4)
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x0
ldc.r4 float32(0x8359185) 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x1b57e965::field_0x0
    ldloca LOCAL_0x2
   ldind.r4
Start_Orphan_a:
     ldsfld int8[] ILGEN_0x1b57e965::field_0x0
    stloc LOCAL_0x0
End_Orphan_a:
    ldarg Arg_0x0
       ldc.i8 0x7f33c94c4c6cd0eb
      conv.r4
       ldc.i8 0xd5224d5a3637c47c
      conv.r8
     rem 
   mul 
  ceq
 conv.r8
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	3
ldc.r8 float64(0xb4e4b4787f7ef458) 
 call float64  ILGEN_0x1b57e965::Method_0xe5b0e4a3(float64 Arg_0x0)
conv.i4
pop
ldc.i4 100
 ret
}
}
