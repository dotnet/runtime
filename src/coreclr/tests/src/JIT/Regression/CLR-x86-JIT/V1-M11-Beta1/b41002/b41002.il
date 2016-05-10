// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x9e8a1a89 {}
.class ILGEN_0x9e8a1a89 {

.method static int32 Method_0xa837be5e(float64 Arg_0x0, unsigned int64 Arg_0x4, unsigned int64 Arg_0x5, unsigned int16 Arg_0x6) {
.maxstack	17

 .locals (unsigned int8 local_0x0,int16[] local_0x1)

 ldc.i4 0x6e6e477d  
 stloc local_0x0
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x1

 ldarg Arg_0x5
 conv.i4
 brtrue Branch_0x8
 ldarg Arg_0x5
 conv.u
 br Branch_0x9
Branch_0x8:
 ldloc local_0x0
Branch_0x9:

 ldc.i8 0x709c208255b724c2
 conv.i
 bge Branch_0x6
 ldarg Arg_0x0
 conv.ovf.i1
 br Branch_0x7
Branch_0x6:
 ldc.i4.4
Branch_0x7:
 conv.u2
Start_Orphan_3:
 ldc.i4 0x183b486e
 conv.ovf.u2
 starg Arg_0x6
End_Orphan_3:
 ldc.i4.6
 beq Branch_0x4
 ldc.i4.5
 br Branch_0x5
Branch_0x4:
Start_Orphan_c:
 ldloc local_0x1
 ldc.i4.1
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_c:
Start_Orphan_e:
 ldc.i8 0x96c64eb8783de1b5
 ldarg Arg_0x4
 div.un
 pop
End_Orphan_e:
 ldarg Arg_0x4
 conv.u2
 conv.u8
 conv.u
Branch_0x5:
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0x1b0df59eabdf62d6) 
ldc.i8 0x2e99e61f99ab9353c 
ldc.i8 0xaeeb4885ecf18e1d8 
ldc.i4 0x1f8827cb 

call int32 ILGEN_0x9e8a1a89::Method_0xa837be5e(float64 Arg_0x0, unsigned int64 Arg_0x4, unsigned int64 Arg_0x5, unsigned int16 Arg_0x6)
pop

  leave a1
} catch [mscorlib]System.OverflowException {
  pop
  leave a1
}
a1:
ldc.i4 100
 ret
}

}
