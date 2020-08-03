// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1c9344b9.exe {}
.class ILGEN_0x1c9344b9 {
.field static	int16[] field_0x0
.method static int32 Method_0xcf54fbcb() {
.maxstack	13
.locals (int8 local_0x3,unsigned int8[] local_0x4,native int local_0x8)
 ldc.i4 0xad970933 
 stloc local_0x3
 ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x4
 ldc.i4 0x3bed5030 
 stloc local_0x8

ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1c9344b9::field_0x0

Start_Orphan_0:
 ldsfld int16[] ILGEN_0x1c9344b9::field_0x0
 ldloc local_0x3
 ldloc local_0x8
 blt.un Branch_0x2
 ldloc local_0x4
 ldlen
 ldc.i4.1
 sub
 br Branch_0x3
 Branch_0x2:
 ldsfld int16[] ILGEN_0x1c9344b9::field_0x0
 ldlen
 ldc.i4.1
 sub
 Branch_0x3:
 ldelema [mscorlib]System.Int16

Start_Orphan_3:
 ldsfld int16[] ILGEN_0x1c9344b9::field_0x0
 ldc.i4 55
 ldc.i4 0x6cedf978
 stelem.i2
 Branch_0x5:
End_Orphan_3:

 ldloc local_0x8

Start_Orphan_6:
 ldloca local_0x8
 pop
End_Orphan_6:

 stind.i2
End_Orphan_0:

ldc.i4 55
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
call int32 ILGEN_0x1c9344b9::Method_0xcf54fbcb()
pop
ldc.i4 100
 ret
}

}
