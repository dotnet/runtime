// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b49104' {}
.assembly extern xunit.core {}
.class ILGEN_0x5fc83106 {

.method static int32 Method_0x959f4c04(unsigned int8 Arg_0x0, int32 Arg_0x1, unsigned int16 Arg_0x2, float64 Arg_0x3, int8 Arg_0x4, unsigned int64 Arg_0x5) {
.maxstack	17

Start_Orphan_0:
Start_Orphan_1:
 ldarg Arg_0x0
 ldc.i4.5
 ble Branch_0x6
 ldarg Arg_0x3
 br Branch_0x7
 Branch_0x6:
 ldc.r4 float32(0xcb06e02f)
 Branch_0x7:
 call float64 [mscorlib]System.Math::Round(float64)
 ldc.i4.1
 ldc.i4.5
 conv.u2
 blt.un Branch_0x8
 ldarg Arg_0x5
 conv.r4
 br Branch_0x9
 Branch_0x8:
 ldc.r8 float64(0x6d29eb5b26407cf6)
 call float64 [mscorlib]System.Math::Round(float64)
 Branch_0x9:
 clt
 ldc.i8 0xfdf7b2317aa60f3a
 conv.ovf.u1.un
 clt
 pop
End_Orphan_1:
 ldc.r4 float32(0x4100fc82)
 conv.ovf.u
 ldc.r4 float32(0x7bff9582)
 call float32 [mscorlib]System.Math::Abs(float32)
 conv.ovf.i8
 conv.ovf.i.un
Start_Orphan_e:
 ldarga Arg_0x4
 ldc.i4.7
 stind.i1
End_Orphan_e:
 ldc.i4 0x1b6c73ec
 conv.ovf.i2.un
 conv.ovf.i2
 conv.ovf.u4.un
 mul
 conv.ovf.i4
Start_Orphan_14:
 ldarg Arg_0x0
 starg Arg_0x4
End_Orphan_14:
 ldc.i4.3
 conv.r4
 ldarg Arg_0x1
 conv.r8
 add 
 conv.ovf.u1
Start_Orphan_16:
 ldc.r4 float32(0x4f88e78f)
 conv.ovf.i2
 starg Arg_0x1
End_Orphan_16:
 ldc.i4.m1
 conv.ovf.u2
 conv.ovf.i4.un
 ldarg Arg_0x0
 conv.u1
 conv.ovf.i1.un
 ldc.i4.1
 ldarg Arg_0x1
 ldc.i4.6
 ceq
 sub.ovf.un
 or
 sub.ovf
 sub
 sub.ovf
 blt.un Branch_0x0
 ldc.i4.s 79
Start_Orphan_1e:
 ldc.i4.m1
 starg Arg_0x0
End_Orphan_1e:
 conv.ovf.u4.un
 starg Arg_0x1
 br Branch_0x1
 Branch_0x0:
 ldc.i4 0x97ae206b
 conv.ovf.u1
 conv.i
 conv.i2
 starg Arg_0x0
 Branch_0x1:

End_Orphan_0:
 ldc.i4.m1
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x397381b7 
ldc.i4 0x16b095f6 
ldc.i4 0x9c76be9f 
ldc.r8 float64(0x24867cac7536c46c) 
ldc.i4 0xb3b30f83 
ldc.i8 0xfaff309ab5c2d1d0 

call int32 ILGEN_0x5fc83106::Method_0x959f4c04(unsigned int8 Arg_0x0, int32 Arg_0x1, unsigned int16 Arg_0x2, float64 Arg_0x3, int8 Arg_0x4, unsigned int64 Arg_0x5)
pop
leave EEE
} catch [mscorlib]System.OverflowException {
pop
leave EEE
}
EEE:
ldc.i4 100
 ret
}

}
