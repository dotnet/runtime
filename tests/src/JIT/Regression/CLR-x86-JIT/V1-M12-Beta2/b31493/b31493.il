// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb9cf1080 {}
.class ILGEN_0xb9cf1080 {
.field static	int8 field_0x2
.method static int32 Method_0x7d15() {
.maxstack	19
 ldc.i4 0x5e7f267a 
 stsfld int8 ILGEN_0xb9cf1080::field_0x2

Start_Orphan_0:
 ldc.i4 0x59ff1b83 
 conv.ovf.i1
 conv.u8
 ldsfld int8 ILGEN_0xb9cf1080::field_0x2
 conv.i
 conv.u8
 mul
 pop
End_Orphan_0:

 ldc.i4.6
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0xb9cf1080::Method_0x7d15()
 pop
 leave the_end
 } catch [mscorlib]System.OverflowException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}

}
