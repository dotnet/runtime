// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x213510c0 {}
.class ILGEN_0x213510c0 {

.method static int32 Method_0x6556(unsigned int32 Arg_0x2) {
.maxstack	13

 .locals (int64 local_0x1)

ldc.i8 0x2a24128c2c149e2 
stloc local_0x1

Start_Orphan_0:
 ldc.i4 255
 newarr [mscorlib]System.Single 
 ldarg Arg_0x2
 conv.ovf.u1.un
 ldelema [mscorlib]System.Single
 pop
End_Orphan_0:
 ldc.r4 float32(0x298a7571)
 conv.u1
 newarr [mscorlib]System.UInt32
Start_Orphan_b:
 ldc.i4 255
 newarr [mscorlib]System.Single 
Start_Orphan_c:
 ldloca local_0x1
 ldloc local_0x1
 stind.i8
End_Orphan_c:
 ldc.i4 55
 ldc.r4 55
 stelem.r4
End_Orphan_b:
 ldc.i4.5
Start_Orphan_e:
 ldarg Arg_0x2
 not
 starg Arg_0x2
End_Orphan_e:
 ldelem.u4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x65fd15d7 

call int32 ILGEN_0x213510c0::Method_0x6556( unsigned int32 Arg_0x2)
 pop
 leave the_end
 } catch [mscorlib]System.OverflowException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}

}
