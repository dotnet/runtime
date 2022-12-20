// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b41129' {}
.assembly extern xunit.core {}
.class ILGEN_0x7971e09f {
.field static	unsigned int16[] field_0x1
.field static	int64[] field_0x2
.field static	int8[] field_0x4
.field static	unsigned int8[] field_0x5

.method static int32 Method_0xd939d2c1(int32 Arg_0x0, unsigned int32 Arg_0x1) {
.maxstack	19

 .locals (int64[] local_0x0)

ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x0

ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x7971e09f::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x7971e09f::field_0x5

Start_Orphan_0:
 ldc.r8 float64(0xc7a77190d21bd6ce)
 conv.u
 ldc.i4.6
 conv.ovf.u
 bne.un Branch_0x6

 ldc.r8 float64(0x26d67e257c76c3b9)
 conv.i4
 br Branch_0x7

 Branch_0x6:

 ldc.r4 float32(0xe8547c75)
 conv.ovf.i1
 Branch_0x7:

 conv.i8
 conv.i2
 ldc.i4.3
 conv.ovf.u.un
 ldsfld unsigned int16[] ILGEN_0x7971e09f::field_0x1
 ldarg Arg_0x1
 ldelema [mscorlib]System.UInt16
 ldind.u2
 sub.ovf.un
 ldarg Arg_0x1
 conv.ovf.i.un
 conv.i
 ldc.i4.5
 conv.u
 ldc.i4.s -90
 ldarg Arg_0x1
 mul.ovf.un
 add
 rem
 mul.ovf.un
 pop
 conv.i4
Start_Orphan_10:
 ldsfld unsigned int8[] ILGEN_0x7971e09f::field_0x5
 ldc.i4.4
 ldelema [mscorlib]System.Byte
 ldarg Arg_0x0
 conv.r4
 conv.u8
 ldarg Arg_0x0
 conv.ovf.i8
 ldc.r4 float32(0x7dd2630d)
 conv.i8
 rem
 clt
 stind.i1
End_Orphan_10:
 ldc.i4.5
 conv.u
 beq Branch_0x0

Start_Orphan_1a:
 ldsfld unsigned int8[] ILGEN_0x7971e09f::field_0x5
 ldc.i4.4
 ldelema [mscorlib]System.Byte
 ldarg Arg_0x0
 conv.ovf.i2
 stind.i1
End_Orphan_1a:
 ldc.r8 float64(0x15ad760a164351df)
 conv.u1
 newarr [mscorlib]System.SByte
 stsfld int8[] ILGEN_0x7971e09f::field_0x4
 br Branch_0x1

 Branch_0x0:

 ldloc local_0x0
 ldc.i4.7
 ldelema [mscorlib]System.Int64
Start_Orphan_2e:
 ldarg Arg_0x1
 conv.u1
 newarr [mscorlib]System.Int64
 stsfld int64[] ILGEN_0x7971e09f::field_0x2
End_Orphan_2e:
 ldc.i4.0
 conv.ovf.u8.un
 ldarg Arg_0x0
 conv.i8
 and
 stind.i8
 Branch_0x1:

End_Orphan_0:
ldc.i4 55
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x2000c176 
ldc.i4 0x670f86b2 

call int32 ILGEN_0x7971e09f::Method_0xd939d2c1(int32 Arg_0x0, unsigned int32 Arg_0x1)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
