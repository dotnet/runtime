// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1aecfae6 {}
.class ILGEN_0x1aecfae6 {

//BEGIN STATIC FIELDS
.field static	int16[] field_0x0
//END STATIC FIELDS
.method static float32 Method_0x6bf3() {
.maxstack	5

 .locals ()
//BEGIN INIT LOCALS

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1aecfae6::field_0x0

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 nop
End_Orphan_0:
 ldc.i4.0
 ldc.i4.5
 bge.un Branch_0x2

 ldc.i4 0x53b06b1f
 br Branch_0x3

 Branch_0x2:

 ldc.i4.0
 Branch_0x3:

 ldc.i4.3
 ldc.i4.m1
 mul.ovf
 ble Branch_0x0

Start_Orphan_1:
 nop
End_Orphan_1:
 ldc.i4.1
 ldc.i4.1
 bge.un Branch_0x4

 ldc.r4 float32(0x8325d6)
 br Branch_0x5

 Branch_0x4:

 ldc.r4 float32(0x119710e7)
 Branch_0x5:

 br Branch_0x1

 Branch_0x0:

Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.r8 float64(0xf9b57269313d3e)
 ldc.r8 float64(0x4f0a3bc01d007e0b)
 bgt.un Branch_0x6

 ldc.r4 float32(0x57013385)
 br Branch_0x7

 Branch_0x6:

 ldc.r4 float32(0x27216a8)
 Branch_0x7:

 Branch_0x1:

ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20

call float32 ILGEN_0x1aecfae6::Method_0x6bf3()
conv.i4
ldc.i4 100
add
 ret
}

}
