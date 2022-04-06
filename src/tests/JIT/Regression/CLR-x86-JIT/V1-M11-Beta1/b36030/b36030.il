// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b36030' {}
.assembly extern xunit.core {}
.class ILGEN_0x68f5b414 {

.field static	float64[] field_0x0

.method static float32 Method_0x51d(unsigned int64 Arg_0x1, float32 Arg_0x3) {
.maxstack	17
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x68f5b414::field_0x0

 ldarg Arg_0x1
 conv.r4
 ldarg Arg_0x3
 rem
Start_Orphan_d:
 ldsfld float64[] ILGEN_0x68f5b414::field_0x0
 ldc.i4.3
 ldelem.r8
 pop
End_Orphan_d:
Start_Orphan_10:
 ldsfld float64[] ILGEN_0x68f5b414::field_0x0
 ldc.i4.3
 ldelem.r8
 pop
End_Orphan_10:
 conv.r4
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
ldc.i8 0x1dc3638c3d8a55be 
ldc.r4 float32(0x13ce587e) 

call float32 ILGEN_0x68f5b414::Method_0x51d(unsigned int64 Arg_0x1,float32 Arg_0x3)
conv.i4
ldc.i4 100
add
 ret
}

}
