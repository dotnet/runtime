// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


 // Max Args = 0 

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x16802ac7 {}
.assembly extern xunit.core {}
.class ILGEN_0x16802ac7 {

//BEGIN STATIC FIELDS
.field static	unsigned int8[] field_0x0
.field static	unsigned int8[] field_0x1
.field static	int16[] field_0x2
.field static	unsigned int16 field_0x3
//END STATIC FIELDS
.method static int32 Method_0xaf540f56() {
.maxstack	5

 .locals (float32[] local_0x0,int16[] local_0x1)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x1

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x16802ac7::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x16802ac7::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x16802ac7::field_0x2
ldc.i4 0xd825eacc 
stsfld unsigned int16 ILGEN_0x16802ac7::field_0x3

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldc.i8 0x9c5b95283912b6ec
 ldc.i8 0x1d8b382aa8944b71
 ble.un Branch_0x0

 nop
 br Branch_0x1

 Branch_0x0:

 nop
 Branch_0x1:

End_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldloc local_0x0
 ldc.i4.3
 ldelema [mscorlib]System.Single
 ldind.r4
Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.r4 float32(0xdce5a77e)
 ldc.r8 float64(0xb72dbae6ec52ad4e)
 rem conv.r4
 clt
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call int32 ILGEN_0x16802ac7::Method_0xaf540f56()
ldc.i4 100
add
 ret
}

}
