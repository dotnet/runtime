// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x64f58bd0 {}
.assembly extern xunit.core {}
.class ILGEN_0x64f58bd0 {

.field static	unsigned int8 field_0x4

.method static int32 Method_0x1bb0(int32 Arg_0x0, unsigned int64 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, unsigned int32 Arg_0x4) {
.maxstack	15

ldc.i4 0x3b63321e 
stsfld unsigned int8 ILGEN_0x64f58bd0::field_0x4

Start_Orphan_1:
 ldarga Arg_0x0
 ldc.i4.7
 stind.i4
End_Orphan_1:
 ldarg Arg_0x2
 conv.u
 ldarg Arg_0x1
 conv.ovf.i2
 ldc.r4 float32(0x62e746dc)
 conv.i
 rem.un
 or
 ldc.i4.4
 conv.ovf.u1.un
 ldc.i8 0x170251429471e93
 conv.ovf.i2.un
 div
Start_Orphan_e:
 ldarga Arg_0x0
 ldc.i4.3
 conv.u1
 stind.i4
End_Orphan_e:
 ldsfld unsigned int8 ILGEN_0x64f58bd0::field_0x4
 conv.ovf.i4.un
 ldarg Arg_0x4
 conv.u4
 ldarg Arg_0x4
 ldarg Arg_0x0
 mul.ovf.un
 sub.ovf
 sub.ovf
 sub
 conv.i
 cgt.un
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.locals (int32)
ldc.i4.1
stloc.0
.try {
ldc.i4 0x37c53932 
ldc.i8 0x27df353a19c93d3 
ldc.r4 float32(0x58d22a34) 
ldc.i4 0x3f371ed2 
ldc.i4 0x33f4f0c 
call int32 ILGEN_0x64f58bd0::Method_0x1bb0(int32 Arg_0x0, unsigned int64 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, unsigned int32 Arg_0x4)
pop
leave SSS
}
catch [mscorlib]System.OverflowException
{
pop
ldc.i4.0
stloc.0
leave SSS
}
SSS:
ldloc.0
ldc.i4 100
add
ret
}

}
