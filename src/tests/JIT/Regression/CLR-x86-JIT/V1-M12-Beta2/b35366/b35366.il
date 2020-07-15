// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x449c7d9 {}
.class ILGEN_0x449c7d9 {

.method static int32 Method_0x13da(unsigned int32 Arg_0x0) {
.maxstack	9

 .locals (int32[] local_0x0,unsigned int8 local_0x2)

 ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x0
 ldc.i4 0x3a441fb6 
 stloc local_0x2

 ldc.r8 float64(0x1fa67b71117c4f19)
 conv.ovf.i

Start_Orphan_4:
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 ldc.i4.4
 ldc.i4 0x258d79be 
 stelem.i2
End_Orphan_4:

 ldc.i4.6
 ldarg Arg_0x0
 sub
 clt.un
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x14c07bb9 
call int32 ILGEN_0x449c7d9::Method_0x13da(unsigned int32 Arg_0x0)
ldc.i4.1
sub
ldc.i4 100
add
ret
}

}
