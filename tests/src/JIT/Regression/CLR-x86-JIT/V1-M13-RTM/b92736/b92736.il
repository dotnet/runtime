// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x50a9a77.exe {}
.class ILGEN_0x50a9a77 {
.field static	unsigned int16 field_0x5
.field static	int64 field_0x6
.method static int32 Method_0xfb76bbce(int32 Arg_0x0) {
.maxstack	17
Start_Orphan_3e:
 ldc.i4.0
 starg Arg_0x0
End_Orphan_3e:
 ldc.i4 54
 brtrue Branch_0x0
 ldc.i4 0xa69bde6d
 br Branch_0x1
Branch_0x0:
 ldc.i4 56
Branch_0x1:
Start_Orphan_5a:
 ldarg Arg_0x0
 ldsfld unsigned int16 ILGEN_0x50a9a77::field_0x5
 shr
 stsfld unsigned int16 ILGEN_0x50a9a77::field_0x5
End_Orphan_5a:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 ldc.i4 0xdab10162 
 call int32 ILGEN_0x50a9a77::Method_0xfb76bbce(int32 Arg_0x0)
 ldc.i4.s 44
 add
 ret
}
}