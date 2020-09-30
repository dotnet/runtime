// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x37ae0554 {}
.class ILGEN_0x37ae0554 {
.method static int32 Method_0xb77ea7c1(int16 Arg_0x0, int16 Arg_0x1) {
.maxstack	17

 .locals (unsigned int32 local_0x0,float64[] local_0x3,unsigned int32 local_0x4)

ldc.i4 0xe01d8554 
stloc local_0x0
ldc.i4 0x74a417b 
stloc local_0x4

 ldc.i4.1
 conv.ovf.i8
 ldc.i4.4
 conv.u8
 mul
 conv.r4
 ldc.i8 0xa19f29e4a5b3c96
 conv.ovf.u2
 ldc.i4 255
 newarr [mscorlib]System.Double
 ldc.i4.8
 ldelema [mscorlib]System.Double
 ldind.r8

Start_Orphan_48:
 ldloca local_0x4
 ldloc local_0x0
 stind.i4
End_Orphan_48:
 
 ldc.i8 0x8628f4b7feba3ca6
 conv.r8
 cgt
 clt
 conv.ovf.i2
 conv.r4
 cgt
 ldc.r8 float64(0x9d31db4130caaff6)
 conv.ovf.u
 ceq
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x456492df 
ldc.i4 0xd3975c69 

call int32 ILGEN_0x37ae0554::Method_0xb77ea7c1(int16 Arg_0x0, int16 Arg_0x1)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
