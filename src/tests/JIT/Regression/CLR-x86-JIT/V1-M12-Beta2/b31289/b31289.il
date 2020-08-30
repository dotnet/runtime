// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x59e058c0 {}
.class ILGEN_0x59e058c0 {

.field static	unsigned int16 field_0x0
.method static int32 Method_0x2dd7(unsigned int64 Arg_0x3) {
.maxstack	17

 .locals (int64[] local_0x0,unsigned int8 local_0x1,unsigned int8[] local_0x2)

ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x0
ldc.i4 0x76c72543 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x2

ldc.i4 0xf087595 
stsfld unsigned int16 ILGEN_0x59e058c0::field_0x0


Start_Orphan_0:
 ldsflda unsigned int16 ILGEN_0x59e058c0::field_0x0
 ldc.r4 float32(0x279a2607)
 conv.u1
 stind.i2
End_Orphan_0:
Start_Orphan_1:
 ldarg Arg_0x3
  conv.ovf.i1
 conv.u1
 newarr [mscorlib]System.Int64
 pop
End_Orphan_1:
Start_Orphan_a:
 ldloc local_0x0
 ldc.i4.2
 ldelema [mscorlib]System.Int64
 ldarg Arg_0x3
 stind.i8
End_Orphan_a:
 ldc.i8 3
 ldloc local_0x0
 ldc.i4.6
 ldelem.i8
 neg
 conv.i8
 or
 conv.u1
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x65d25f3b4fa63daa 

call int32 ILGEN_0x59e058c0::Method_0x2dd7(unsigned int64 Arg_0x3)
pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
 ldc.i4 100
ret
}

}
