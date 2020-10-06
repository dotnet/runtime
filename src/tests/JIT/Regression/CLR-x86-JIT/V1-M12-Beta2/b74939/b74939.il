// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b74939 { }
.class ILGEN_0xd191b6a {
.field static	int8 field_0x0
.field static	int16[] field_0x4
.method static unsigned int32  Method_0xc18cf9c3() {
.maxstack	9
.locals (native int LOCAL_0x6,int64[] LOCAL_0x7)
 ldc.i4 0xf645b517 
 stloc LOCAL_0x6
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x7
 ldc.i4 0xab654366 
 stsfld int8 ILGEN_0xd191b6a::field_0x0
 ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xd191b6a::field_0x4
Start_Orphan_0:
   ldc.i8 0xe8cfd6da416c0588
  conv.ovf.i8.un
  ldloc LOCAL_0x7
   ldsflda int8 ILGEN_0xd191b6a::field_0x0
  ldind.i1
 ldelem.i8
 ble.un Branch_0x0
   ldc.i4.m1
   ldloc LOCAL_0x6
  div.un
 pop
 br Branch_0x1
 Branch_0x0:
  ldsfld int16[] ILGEN_0xd191b6a::field_0x4
   ldc.i4 55
  ldc.i4 4
 stelem.i2
Branch_0x1:
End_Orphan_0:
ldc.i4 55
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	1
.try {
 call unsigned int32  ILGEN_0xd191b6a::Method_0xc18cf9c3()
 conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}

}
