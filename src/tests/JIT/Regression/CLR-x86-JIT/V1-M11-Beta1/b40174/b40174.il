// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x587ddc1e {}
.class ILGEN_0x587ddc1e {
.method static int32 Main() {
.entrypoint
.maxstack	20
.locals (int32)
ldc.i4.1
stloc.0
.try {
Start_Orphan_1:
 ldc.i4 0xc5 
 newarr [mscorlib]System.UInt64
 ldc.i4 0x71
// ldc.i4 0xb9
// bgt.un Branch_0x4
 brtrue Branch_0x4
 ldc.i4.3
 br Branch_0x5
Branch_0x4:
 ldc.i4.m1
Branch_0x5:
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1:
 leave outofhere
} catch [mscorlib]System.IndexOutOfRangeException
{
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
