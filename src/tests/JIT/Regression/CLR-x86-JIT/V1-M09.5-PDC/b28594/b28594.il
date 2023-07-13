// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b28594' {}
.class public ILGEN_0xcb19b8b8 {

.field static	int64 field_0x2
.field static	int8 field_0x6

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (float64 local_0x0,unsigned int16[] local_0x2,unsigned int16 local_0x3,unsigned int8 local_0x5,int16 local_0x6)
 .try {
ldc.r8 float64(0x5eb66fb133287adb) 
stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x2
ldc.i4 0x549c64fc 
stloc local_0x3
ldc.i4 0x1984913 
stloc local_0x5
ldc.i4 0x5bf3367a 
stloc local_0x6

ldc.i8 0xf4173d6cf840e1 
stsfld int64 ILGEN_0xcb19b8b8::field_0x2
ldc.i4 0x163a1450 
stsfld int8 ILGEN_0xcb19b8b8::field_0x6

 nop
 nop
 nop
 ldc.r4 float32(0x4be23253)
 ldc.i4.4
 ldc.i4.7
 sub.ovf.un
 conv.r4
 sub
 conv.i8
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 conv.ovf.i8.un
 ldloc local_0x6
 conv.ovf.u8.un
 or
Start_Orphan_9:
 ldsflda int64 ILGEN_0xcb19b8b8::field_0x2
 ldc.i8 0x7f7b1f3a1b8956c1
 stind.i8
End_Orphan_9:
 ldc.i4.3
 conv.ovf.i8
 ldloc local_0x0
 ldloca local_0x0
 ldind.r8
 cgt
 shl
 add
 sub
 conv.r.un
 ldloc local_0x0
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 div
 conv.r8
 rem
Start_Orphan_17:
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 rem.un
 pop
End_Orphan_17:
 ldloc local_0x0
 conv.r8
 add
 conv.r8
 sub
Start_Orphan_1d:
 ldloc local_0x2
 ldloc local_0x3
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_1d:
 ldsfld int64 ILGEN_0xcb19b8b8::field_0x2
 conv.r8
 ldloc local_0x5
 conv.r.un
 ldloc local_0x0
 rem
 ldloca local_0x0
 ldind.r8
Start_Orphan_2c:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldsfld int8 ILGEN_0xcb19b8b8::field_0x6
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_2c:
 mul
 add
 ceq
 pop
 leave the_end
} catch [mscorlib]System.OverflowException {
  pop
  leave the_end
}
the_end:
ldc.i4.0
ldc.i4 100
add
ret
}
}
