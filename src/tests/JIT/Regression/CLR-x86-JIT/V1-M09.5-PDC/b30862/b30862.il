// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b30862' {}
.assembly extern xunit.core {}
.class ILGEN_0xe741640 {
.field static	int64 field_0x1
.field static	float32[] field_0x4

.method static int32 Method_0x6401() {
.maxstack	21

 .locals (int32 local_0x0,int8[] local_0x3,float32[] local_0x4)
 ldc.i4.0
 stloc.0
 ldnull
 dup
 stloc.1
 stloc.2
.try {
ldc.i4 55
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x4

ldc.i8 0x3249597d7fdd643c 
stsfld int64 ILGEN_0xe741640::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0xe741640::field_0x4


Start_Orphan_0:
 ldloca local_0x0
 ldsfld float32[] ILGEN_0xe741640::field_0x4
 ldloc local_0x0
 ldelem.r4
 conv.i
Start_Orphan_19:
 ldc.r4 float32(0x579141ee)
 conv.ovf.i8
Start_Orphan_1f:
 ldloc local_0x4
 ldc.i4.7
 ldc.r4 float32(0x15db1dbe)
 stelem.r4
End_Orphan_1f:
 ldc.i8 0x8
 ldsfld int64 ILGEN_0xe741640::field_0x1
 sub
 sub.ovf.un
 pop
End_Orphan_19:
 ldloc local_0x0
 ldsfld int64 ILGEN_0xe741640::field_0x1
 conv.i4
 div
 add
 stind.i4
End_Orphan_0:
 ldloc local_0x3
 ldlen
 pop
 leave the_end
 } catch [mscorlib]System.OverflowException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 0
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
 call int32 ILGEN_0xe741640::Method_0x6401()
ldc.i4 100
add
ret
}

}
