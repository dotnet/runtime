// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x50013a8e {}
.class ILGEN_0x50013a8e {
.method static int32 Main() {
.entrypoint
.maxstack	20
 .locals (float64[] local_0x6,float64[] local_0x7,unsigned int16[] local_0x8,int8 local_0xd)
 .try {
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x6
 ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x7
 ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x8 
 ldc.i4 0xb7ffc5ff 
 stloc local_0xd

Start_Orphan_0:
 ldc.i4.s -54
 ldloc local_0xd
 sub
 conv.ovf.u8
 ldc.i4 55
 ldc.i4 56
 bge.un Branch_0x4
 ldc.i4 0xb7ffc5ff 
 br Branch_0x5
Branch_0x4:
 ldc.i4.3
Branch_0x5:
 conv.i8
 bgt.un Branch_0x0
Branch_0x0:
End_Orphan_0:
 ldc.i4.4
	pop
	leave X
} catch [mscorlib]System.OverflowException
{
	pop
	leave X
}
X:
ldc.i4 100
 ret
}
}
