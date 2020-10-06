// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1f290143 {}
.class ILGEN_0x1f290143 {

.method static int32 Method_0x8252f06e(float64 Arg_0x0, unsigned int16 Arg_0x1, unsigned int32 Arg_0x2, unsigned int8 Arg_0x3, float32 Arg_0x4, unsigned int64 Arg_0x5, int32 Arg_0x6, unsigned int64 Arg_0x7) {
.maxstack	17

 .locals (unsigned int64 local_0x1,float64 local_0x2)
ldc.i8 0x92a3433282374d37 
stloc local_0x1
ldc.r8 float64(0x3d5a6bbe30ee427d) 
stloc local_0x2

Start_Orphan_9:
 ldloca local_0x1
 pop
End_Orphan_9:
 ldloca local_0x2
 ldind.r8
 pop

 ldarg Arg_0x5
 conv.ovf.i1
 ldloc local_0x1
 ldarg Arg_0x6
 conv.u8
 ldloc local_0x1
 conv.ovf.i8.un
 sub
 add
 conv.u2
 shr
 ldarg Arg_0x5
 conv.i2
 rem.un
 pop

 ldarg Arg_0x4
 conv.u1
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0x4a659edbf4516401) 
ldc.i4 0x2dcea3b7 
ldc.i4 0x9b1e3ef6 
ldc.i4 0x1dfe5c6f 
ldc.r4 float32(0x16423c9e) 
ldc.i8 0xbfd3afc0a7032211 
ldc.i4 0x360104ef 
ldc.i8 0x237a327b995a61b3 

call int32 ILGEN_0x1f290143::Method_0x8252f06e(float64 Arg_0x0, unsigned int16 Arg_0x1, unsigned int32 Arg_0x2, unsigned int8 Arg_0x3, float32 Arg_0x4, unsigned int64 Arg_0x5, int32 Arg_0x6, unsigned int64 Arg_0x7)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
