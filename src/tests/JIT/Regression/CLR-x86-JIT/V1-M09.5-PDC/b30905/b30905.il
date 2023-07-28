// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly 'b30905' {}
.class public ILGEN_0xe696696 {

.field static	int32 field_0x0
.field static	float64 field_0x5
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (int16 local_0x4,unsigned int64[] local_0x5,int8 local_0x6)
 .try {
 
 ldc.i4 0x68491820 
 stloc local_0x4
 ldc.i4 2550
 newarr [mscorlib]System.UInt64 
 stloc local_0x5
 ldc.i4 0xe5 
 stloc local_0x6

 ldc.i4 0x6bdc6c33
 stsfld int32 ILGEN_0xe696696::field_0x0

 ldc.r8 float64(0x7a4660466bdc6c33) 
 stsfld float64 ILGEN_0xe696696::field_0x5

Start_Orphan_d:

Start_Orphan_12:
 ldloca local_0x6
 ldc.i4.m1
 stind.i1
End_Orphan_12:

 ldc.i4.4
 ldc.i4.3
 clt
 conv.i
 ldloc local_0x5
 ldc.i4.7
 ldelema [mscorlib]System.UInt64
 ldind.u8
 conv.ovf.i1.un
ldc.i4 5
add
 div
 conv.r4
 pop
End_Orphan_d:

 ldc.r4 float32(0x68d751f)
ldc.i4 0x6bdc6c33
ldc.i4 0x68491820 
 shr.un
 ldc.i4.3
 conv.ovf.u
 add
 conv.r4
 rem
 ckfinite
 ldc.i8 0x59f0504123526d0e
 conv.i1
 ldc.i4.1
 ldc.i4.0
 sub
 conv.ovf.u2.un
 clt.un

Start_Orphan_17:
 ldloca local_0x4
 ldc.i4.4
 conv.i2
 stind.i2
End_Orphan_17:

 ldc.i4 0x70b92997
 conv.u1
 newarr [mscorlib]System.UInt32
 ldlen
 conv.ovf.u4.un
 mul.ovf
 conv.r4
 div
 conv.u4
 ldc.i4.3
 conv.ovf.i8
 ldc.r8 float64(0x7a4660466bdc6c33) 
 conv.u8
 xor
 dup
 call void [System.Console]System.Console::WriteLine(int64)
 conv.ovf.i2
  ldsfld float64 ILGEN_0xe696696::field_0x5
 ldc.r8 float64(0x7fa9e15a64757)
 ceq
 ldc.i8 0xef574
 conv.ovf.i
 mul
 ldc.i4.2
 conv.i1
 ldc.i4.5
 conv.i
 add
 rem
 ldc.r4 float32(0x96c6e49)
 conv.ovf.u
 ldc.i8 0x5a0f361b1b4149af
 conv.r4
 ldc.r4 float32(0x11f4671a)
 ckfinite
 cgt

Start_Orphan_29:
 ldloc local_0x5
 ldloc local_0x6
 conv.u1
 ldc.i8 0xc2
 stelem.i8
End_Orphan_29:

 sub
 rem
 conv.i2
 sub
 ceq
 pop
 leave out_of_here
 } catch [mscorlib]System.OverflowException {
 pop 
 leave out_of_here
 }
out_of_here:
ldc.i4.0
ldc.i4 100
add
ret
}
}
