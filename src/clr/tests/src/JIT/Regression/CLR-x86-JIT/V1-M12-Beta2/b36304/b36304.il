// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x7ff151ef {}
.class ILGEN_0x7ff151ef {
.method static float32 Method_0x53ea(float64 Arg_0x0, unsigned int64 Arg_0x5) {
.maxstack	15
Start_Orphan_0:
 ldarga Arg_0x0
 pop
End_Orphan_0:
Start_Orphan_8:
 ldc.i4.m1
 conv.u8
 starg Arg_0x5
End_Orphan_8:
 ldarg Arg_0x0
 conv.r4
 ldarg Arg_0x5
 conv.ovf.i4
 conv.r4
 clt
 conv.u1
 newarr [mscorlib]System.Double
 ldc.r8 float64(0x5bd8661fa6553b7)
 conv.r4
 neg
 ldc.r4 float32(0x34c6449) 
 clt
 ldelem.r8
 conv.r4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.locals (int32)
ldc.i4.1
stloc.0
.try {
	ldc.r8 float64(0x45c1ef45f4b2669) 
	ldc.i8 0x54523a2666bccb 

	call float32 ILGEN_0x7ff151ef::Method_0x53ea(float64 Arg_0x0, unsigned int64 Arg_0x5)
	conv.i4
	pop
	
	leave outofhere
 } catch [mscorlib]System.OverflowException {
 	pop
	ldc.i4.0
	stloc.0
 	leave outofhere
 }
 outofhere:
 ldloc.0
ldc.i4 100
add
 ret
}

}
